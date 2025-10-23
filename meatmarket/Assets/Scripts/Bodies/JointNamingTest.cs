using UnityEngine;

/// <summary>
/// Test script to verify the joint naming logic works correctly.
/// This can be removed after testing is complete.
/// </summary>
public class JointNamingTest : MonoBehaviour
{
    [ContextMenu("Test Joint Naming")]
    public void TestJointNaming()
    {
        Debug.Log("=== JOINT NAMING TEST ===");
        
        // Test arm joints
        TestJoint(Limb.LeftArm, CutSection.ShoulderOrHip, "Shoulder");
        TestJoint(Limb.RightArm, CutSection.ElbowOrKnee, "Elbow");
        TestJoint(Limb.LeftArm, CutSection.WristOrAnkle, "Wrist");
        
        // Test leg joints
        TestJoint(Limb.LeftLeg, CutSection.ShoulderOrHip, "Hip");
        TestJoint(Limb.RightLeg, CutSection.ElbowOrKnee, "Knee");
        TestJoint(Limb.LeftLeg, CutSection.WristOrAnkle, "Ankle");
        
        // Test special cases
        TestJoint(Limb.None, CutSection.Neck, "Neck");
        TestJoint(Limb.LeftArm, CutSection.Neck, "Neck");
        TestJoint(Limb.None, CutSection.TorsoMiddle, "TorsoMiddle");
        TestJoint(Limb.RightLeg, CutSection.TorsoMiddle, "TorsoMiddle");
        
        Debug.Log("=== TEST COMPLETE ===");
    }
    
    private void TestJoint(Limb limb, CutSection section, string expected)
    {
        string result = CutContext.GetSpecificJointName(limb, section);
        bool passed = result == expected;
        string status = passed ? "✓ PASS" : "✗ FAIL";
        
        Debug.Log($"{status}: {limb} + {section} = '{result}' (expected '{expected}')");
        
        if (!passed)
        {
            Debug.LogError($"JOINT NAMING TEST FAILED: Expected '{expected}', got '{result}'");
        }
    }
}







