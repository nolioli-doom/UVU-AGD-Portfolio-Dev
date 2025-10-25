using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Spawns customers irregularly over 3 minutes in a defined waiting area.
/// Customers wander around with floating order summaries.
/// </summary>
public class CustomerSpawner : MonoBehaviour
{
    [Header("Spawning Configuration")]
    [Tooltip("Total time window for all customers to spawn (seconds)")]
    public float spawnWindowSeconds = 180f; // 3 minutes
    
    [Tooltip("Day index for difficulty settings (0 = first day)")]
    public int dayIndex = 0;
    
    [Tooltip("Minimum time between spawns (seconds)")]
    public float minSpawnInterval = 5f;
    
    [Tooltip("Maximum time between spawns (seconds)")]
    public float maxSpawnInterval = 25f;
    
    [Header("Waiting Area Boundaries")]
    [Tooltip("Center point of the waiting area")]
    public Vector3 waitingAreaCenter = new Vector3(0, 0, 0);
    
    [Tooltip("Size of the waiting area (width, height, depth)")]
    public Vector3 waitingAreaSize = new Vector3(10, 2, 8);
    
    [Tooltip("Y position where customers spawn (ground level)")]
    public float spawnYPosition = 0f;
    
    [Header("Spawn Point")]
    [Tooltip("Transform where all customers will spawn (e.g., front door)")]
    public Transform spawnPoint;
    
    [Header("Customer Prefabs")]
    [Tooltip("Customer prefabs to spawn from (should have CustomerVisual script)")]
    public List<GameObject> customerPrefabs = new List<GameObject>();
    
    [Tooltip("If no prefabs assigned, will create primitive customers")]
    public bool usePrimitiveCustomers = true;
    
    [Header("Order Generation")]
    [Tooltip("Order generator for creating customer orders")]
    public OrderGenerator orderGenerator;
    
    [Tooltip("Order manager to store generated orders")]
    public OrderManagerSO orderManager;
    
    [Header("Debug")]
    public bool logSpawns = true;
    public bool showWaitingAreaGizmo = true;
    
    [Header("Events")]
    public UnityEvent OnAllCustomersSpawned;
    public UnityEvent<GameObject> OnCustomerSpawned;
    
    [Header("Delayed Start")]
    [Tooltip("Delay before starting to spawn customers (seconds)")]
    public float startDelay = 0f;
    
    [Tooltip("Unity event fired after the start delay")]
    public UnityEvent OnStartDelayComplete;
    
    private List<GameObject> spawnedCustomers = new List<GameObject>();
    private Coroutine spawningCoroutine;
    private bool hasStartedSpawning = false;
    
    // Pre-generated round data
    private List<CustomerOrder> preGeneratedOrders = new List<CustomerOrder>();
    private int nextOrderIndex = 0;
    private int totalCustomersToSpawn = 0; // Set by difficulty settings
    
    void Start()
    {
        // Auto-find components if not assigned
        if (orderGenerator == null)
            orderGenerator = FindObjectOfType<OrderGenerator>();
        
        if (orderManager == null)
            orderManager = FindObjectOfType<OrderManagerSO>();
        
        // Generate the full round at start
        GenerateFullRound();
        
        // Auto-start spawning after a brief delay
        StartCoroutine(AutoStartSpawning());
    }
    
    /// <summary>
    /// Generate the full round of orders at game start using difficulty settings
    /// </summary>
    private void GenerateFullRound()
    {
        if (orderGenerator == null)
        {
            Debug.LogError("[CustomerSpawner] No OrderGenerator assigned!");
            return;
        }
        
        // Generate the full round using OrderGenerator with proper day index
        orderGenerator.GenerateRound(dayIndex);
        var round = orderGenerator.CurrentRound;
        preGeneratedOrders = new List<CustomerOrder>(round.orders);
        
        // Set total customers to spawn from the generated round
        totalCustomersToSpawn = preGeneratedOrders.Count;
        
        // OrderManagerSO is already populated by OrderGenerator.GenerateRound()
        // No need to call SetOrders again here
        
        // Reset order assignment index
        nextOrderIndex = 0;
        
        if (logSpawns) Debug.Log($"[CustomerSpawner] Generated {preGeneratedOrders.Count} orders for day {dayIndex} (difficulty: {orderGenerator.GetDifficultyName(dayIndex)})");
    }
    
    /// <summary>
    /// Auto-start spawning after scene loads
    /// </summary>
    private IEnumerator AutoStartSpawning()
    {
        // Wait a brief moment for scene to fully initialize
        yield return new WaitForSeconds(1f);
        
        Debug.Log("[CustomerSpawner] Auto-starting customer spawning...");
        OnStartDelayComplete?.Invoke();
    }
    
    /// <summary>
    /// Start spawning customers (call this when the day begins)
    /// </summary>
    public void StartSpawning()
    {
        Debug.Log($"[CustomerSpawner] StartSpawning() called - hasStartedSpawning: {hasStartedSpawning}, preGeneratedOrders.Count: {preGeneratedOrders.Count}, startDelay: {startDelay}");
        
        if (hasStartedSpawning)
        {
            Debug.LogWarning("[CustomerSpawner] Already started spawning!");
            return;
        }
        
        if (preGeneratedOrders.Count == 0)
        {
            Debug.LogError("[CustomerSpawner] No pre-generated orders available!");
            return;
        }
        
        hasStartedSpawning = true;
        
        if (startDelay > 0f)
        {
            // Start with delay
            Debug.Log($"[CustomerSpawner] Starting delayed spawning with {startDelay}s delay");
            StartCoroutine(DelayedStartSpawning());
        }
        else
        {
            // Start immediately
            Debug.Log("[CustomerSpawner] Starting immediate spawning");
            spawningCoroutine = StartCoroutine(SpawnCustomersOverTime());
            OnStartDelayComplete?.Invoke();
        }
        
        if (logSpawns) Debug.Log($"[CustomerSpawner] Started spawning {totalCustomersToSpawn} customers over {spawnWindowSeconds} seconds (delay: {startDelay}s)");
    }
    
    /// <summary>
    /// Coroutine for delayed start
    /// </summary>
    private IEnumerator DelayedStartSpawning()
    {
        Debug.Log($"[CustomerSpawner] DelayedStartSpawning() started - waiting {startDelay} seconds...");
        
        if (logSpawns) Debug.Log($"[CustomerSpawner] Waiting {startDelay} seconds before starting to spawn customers...");
        
        yield return new WaitForSeconds(startDelay);
        
        Debug.Log("[CustomerSpawner] Delay complete, firing OnStartDelayComplete event");
        
        // Fire the delay complete event
        OnStartDelayComplete?.Invoke();
        
        Debug.Log("[CustomerSpawner] Starting SpawnCustomersOverTime coroutine");
        
        // Start spawning
        spawningCoroutine = StartCoroutine(SpawnCustomersOverTime());
        
        if (logSpawns) Debug.Log("[CustomerSpawner] Delay complete, now spawning customers!");
    }
    
    /// <summary>
    /// Stop spawning customers
    /// </summary>
    public void StopSpawning()
    {
        if (spawningCoroutine != null)
        {
            StopCoroutine(spawningCoroutine);
            spawningCoroutine = null;
        }
        
        if (logSpawns) Debug.Log("[CustomerSpawner] Stopped spawning customers");
    }
    
    /// <summary>
    /// Spawn all remaining customers immediately
    /// </summary>
    public void SpawnAllRemaining()
    {
        int remaining = totalCustomersToSpawn - spawnedCustomers.Count;
        for (int i = 0; i < remaining; i++)
        {
            SpawnCustomer();
        }
        
        if (logSpawns) Debug.Log($"[CustomerSpawner] Spawned {remaining} remaining customers immediately");
    }
    
    private IEnumerator SpawnCustomersOverTime()
    {
        float elapsedTime = 0f;
        int customersSpawned = 0;
        
        // Spawn first customer immediately
        Debug.Log("[CustomerSpawner] Spawning first customer immediately");
        SpawnCustomer();
        customersSpawned++;
        
        // Continue with random intervals for remaining customers
        while (customersSpawned < totalCustomersToSpawn && elapsedTime < spawnWindowSeconds)
        {
            // Calculate next spawn time (irregular intervals)
            float nextSpawnDelay = Random.Range(minSpawnInterval, maxSpawnInterval);
            
            // Don't exceed the spawn window
            float timeRemaining = spawnWindowSeconds - elapsedTime;
            if (nextSpawnDelay > timeRemaining)
            {
                nextSpawnDelay = timeRemaining;
            }
            
            yield return new WaitForSeconds(nextSpawnDelay);
            
            // Spawn customer
            SpawnCustomer();
            customersSpawned++;
            elapsedTime += nextSpawnDelay;
        }
        
        // Spawn any remaining customers if we're at the end of the window
        while (customersSpawned < totalCustomersToSpawn)
        {
            SpawnCustomer();
            customersSpawned++;
        }
        
        OnAllCustomersSpawned?.Invoke();
        if (logSpawns) Debug.Log($"[CustomerSpawner] Finished spawning all {totalCustomersToSpawn} customers");
    }
    
    private void SpawnCustomer()
    {
        // Use spawn point - error if not assigned
        if (spawnPoint == null)
        {
            Debug.LogError("[CustomerSpawner] Spawn Point not assigned! Please assign a spawn point transform.");
            return;
        }
        
        Vector3 spawnPosition = spawnPoint.position;
        
        GameObject customer;
        
        if (customerPrefabs.Count > 0)
        {
            // Select random prefab from the list
            GameObject selectedPrefab = customerPrefabs[Random.Range(0, customerPrefabs.Count)];
            customer = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
        }
        else if (usePrimitiveCustomers)
        {
            customer = CreatePrimitiveCustomer(spawnPosition);
        }
        else
        {
            Debug.LogError("[CustomerSpawner] No customer prefabs assigned and usePrimitiveCustomers is false!");
            return;
        }
        
        // Set up customer data
        SetupCustomer(customer);
        
        spawnedCustomers.Add(customer);
        OnCustomerSpawned?.Invoke(customer);
        
        if (logSpawns) Debug.Log($"[CustomerSpawner] Spawned customer #{spawnedCustomers.Count} at {spawnPosition}");
    }
    
    private Vector3 GetRandomSpawnPosition()
    {
        float x = Random.Range(-waitingAreaSize.x / 2, waitingAreaSize.x / 2) + waitingAreaCenter.x;
        float z = Random.Range(-waitingAreaSize.z / 2, waitingAreaSize.z / 2) + waitingAreaCenter.z;
        
        return new Vector3(x, spawnYPosition, z);
    }
    
    private GameObject CreatePrimitiveCustomer(Vector3 position)
    {
        // Create a primitive cube as placeholder customer
        GameObject customer = GameObject.CreatePrimitive(PrimitiveType.Cube);
        customer.name = $"Customer_{spawnedCustomers.Count + 1}";
        customer.transform.position = position;
        
        // Add a random color
        Renderer renderer = customer.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = new Color(
                Random.Range(0.3f, 1f),
                Random.Range(0.3f, 1f),
                Random.Range(0.3f, 1f)
            );
        }
        
        return customer;
    }
    
    private void SetupCustomer(GameObject customer)
    {
        // Add CustomerVisual component if it doesn't exist
        CustomerVisual customerVisual = customer.GetComponent<CustomerVisual>();
        if (customerVisual == null)
        {
            customerVisual = customer.AddComponent<CustomerVisual>();
        }
        
        // Set waiting area boundaries for wandering
        customerVisual.waitingAreaCenter = waitingAreaCenter;
        customerVisual.waitingAreaSize = waitingAreaSize;
        
        // Assign next pre-generated order (no repeats)
        CustomerOrder order = GetNextPreGeneratedOrder();
        
        // Set up the customer visual with the order (archetype is already assigned by OrderGenerator)
        customerVisual.SetupCustomer(order, order.archetype);
    }
    
    /// <summary>
    /// Get the next pre-generated order (no repeats)
    /// </summary>
    private CustomerOrder GetNextPreGeneratedOrder()
    {
        if (preGeneratedOrders.Count == 0)
        {
            Debug.LogError("[CustomerSpawner] No pre-generated orders available!");
            return CreateFallbackOrder();
        }
        
        if (nextOrderIndex >= preGeneratedOrders.Count)
        {
            Debug.LogWarning("[CustomerSpawner] All pre-generated orders have been assigned! Creating fallback order.");
            return CreateFallbackOrder();
        }
        
        CustomerOrder originalOrder = preGeneratedOrders[nextOrderIndex];
        
        // Create a deep copy of the order to avoid shared references
        CustomerOrder order = new CustomerOrder
        {
            customerName = originalOrder.customerName,
            items = new List<OrderItem>(originalOrder.items),
            archetype = originalOrder.archetype,
            timeLimitSeconds = originalOrder.timeLimitSeconds,
            tipMultiplier = originalOrder.tipMultiplier,
            wasteSensitivity = originalOrder.wasteSensitivity
        };
        
        nextOrderIndex++;
        
        if (logSpawns) Debug.Log($"[CustomerSpawner] Assigned order {nextOrderIndex}/{preGeneratedOrders.Count} to customer: {order.customerName} with {order.items?.Count ?? 0} pieces (Order ID: {order.GetHashCode()}) - Day {dayIndex} ({orderGenerator.GetDifficultyName(dayIndex)})");
        
        return order;
    }
    
    /// <summary>
    /// Create a fallback order if pre-generated orders run out
    /// </summary>
    private CustomerOrder CreateFallbackOrder()
    {
        return CreateTestOrder();
    }
    
    private CustomerOrder CreateTestOrder()
    {
        // Get available species from species definitions
        var availableSpecies = new List<SpeciesType>();
        if (orderGenerator != null && orderGenerator.speciesDefs != null)
        {
            foreach (var speciesDef in orderGenerator.speciesDefs)
            {
                if (speciesDef != null)
                    availableSpecies.Add(speciesDef.species);
            }
        }
        
        // Fallback to all species if no definitions found
        if (availableSpecies.Count == 0)
        {
            availableSpecies.AddRange(new[] { SpeciesType.Cat, SpeciesType.Dog, SpeciesType.Bunny });
        }
        
        // Get available part types
        var availableParts = new List<OrderPartType>();
        if (orderGenerator != null && orderGenerator.speciesDefs != null && orderGenerator.speciesDefs.Count > 0)
        {
            var firstSpecies = orderGenerator.speciesDefs[0];
            if (firstSpecies != null)
            {
                foreach (var yield in firstSpecies.yields)
                {
                    if (!availableParts.Contains(yield.partType))
                        availableParts.Add(yield.partType);
                }
            }
        }
        
        // Fallback part types
        if (availableParts.Count == 0)
        {
            availableParts.AddRange(new[] { OrderPartType.Hand, OrderPartType.Foot, OrderPartType.Head });
        }
        
        var order = new CustomerOrder
        {
            customerName = $"Customer_{spawnedCustomers.Count + 1}",
            items = new List<OrderItem>
            {
                new OrderItem
                {
                    species = availableSpecies[Random.Range(0, availableSpecies.Count)],
                    partType = availableParts[Random.Range(0, availableParts.Count)],
                    quantity = 1,
                    minQuality = QualityTier.Normal
                }
            }
        };
        
        return order;
    }
    
    
    /// <summary>
    /// Get all currently spawned customers
    /// </summary>
    public List<GameObject> GetSpawnedCustomers()
    {
        return new List<GameObject>(spawnedCustomers);
    }
    
    /// <summary>
    /// Check if all customers have been spawned
    /// </summary>
    public bool AreAllCustomersSpawned()
    {
        return spawnedCustomers.Count >= totalCustomersToSpawn;
    }
    
    /// <summary>
    /// Called by RaycastButton OnClick - arranges customers in queue
    /// </summary>
    public void OnWaitingAreaClicked()
    {
        var customerGameObjects = GetSpawnedCustomers();
        
        if (customerGameObjects.Count > 0)
        {
            // Find the CustomerQueueManager
            CustomerQueueManager queueManager = FindObjectOfType<CustomerQueueManager>();
            if (queueManager == null)
            {
                Debug.LogError("[CustomerSpawner] CustomerQueueManager not found in scene");
                return;
            }
            
            // Add only waiting customers to queue
            int addedCount = 0;
            foreach (var customerGO in customerGameObjects)
            {
                // Get the CustomerVisual component from the GameObject
                CustomerVisual customerVisual = customerGO.GetComponent<CustomerVisual>();
                if (customerVisual != null && !customerVisual.IsPinned())
                {
                    if (queueManager.AddCustomerToQueue(customerVisual))
                    {
                        addedCount++;
                    }
                }
                else if (customerVisual != null && customerVisual.IsPinned())
                {
                    if (logSpawns) Debug.Log($"[CustomerSpawner] Skipping pinned customer: {customerVisual.name}");
                }
                else
                {
                    Debug.LogWarning($"[CustomerSpawner] Customer GameObject {customerGO.name} does not have CustomerVisual component");
                }
            }
            
            if (logSpawns) Debug.Log($"[CustomerSpawner] Added {addedCount} customers to queue. Queue now has {queueManager.GetAllQueuedCustomers().Count} customers");
        }
        else
        {
            if (logSpawns) Debug.Log("[CustomerSpawner] No customers to arrange in queue");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showWaitingAreaGizmo) return;
        
        // Draw waiting area boundary
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(waitingAreaCenter, waitingAreaSize);
        
        // Draw waiting area floor
        Gizmos.color = new Color(1f, 1f, 0f, 0.1f); // Semi-transparent yellow
        Gizmos.DrawCube(waitingAreaCenter, waitingAreaSize);
        
        // Draw spawn point
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
            Gizmos.DrawWireCube(spawnPoint.position, Vector3.one * 0.3f);
        }
        
        // Draw center point
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(waitingAreaCenter, 0.3f);
        
        // Draw corner markers
        Gizmos.color = Color.cyan;
        Vector3 halfSize = waitingAreaSize * 0.5f;
        Vector3[] corners = {
            waitingAreaCenter + new Vector3(-halfSize.x, 0, -halfSize.z),
            waitingAreaCenter + new Vector3(halfSize.x, 0, -halfSize.z),
            waitingAreaCenter + new Vector3(halfSize.x, 0, halfSize.z),
            waitingAreaCenter + new Vector3(-halfSize.x, 0, halfSize.z)
        };
        
        for (int i = 0; i < corners.Length; i++)
        {
            Gizmos.DrawWireSphere(corners[i], 0.15f);
            Gizmos.DrawLine(corners[i], corners[(i + 1) % corners.Length]);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showWaitingAreaGizmo) return;
        
        // Draw detailed info when selected
        Gizmos.color = Color.white;
        Vector3 labelPos = waitingAreaCenter + Vector3.up * (waitingAreaSize.y * 0.5f + 1f);
        
        // Draw spawn area info
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(waitingAreaCenter, new Vector3(waitingAreaSize.x, 0.1f, waitingAreaSize.z));
        
        // Draw spawn height line
        Gizmos.color = Color.blue;
        Vector3 spawnHeightPos = new Vector3(waitingAreaCenter.x, spawnYPosition, waitingAreaCenter.z);
        Gizmos.DrawWireSphere(spawnHeightPos, 0.5f);
        
        // Draw spawn height plane
        Gizmos.color = new Color(0f, 0f, 1f, 0.05f);
        Gizmos.DrawCube(spawnHeightPos, new Vector3(waitingAreaSize.x, 0.01f, waitingAreaSize.z));
    }
}
