#if UNITY_EDITOR
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UIInspectorReport
{
    [MenuItem("Tools/UI/Report Order UI")]
    public static void Report()
    {
        var sb = new StringBuilder();
        var canvas = Object.FindFirstObjectByType<Canvas>();
        var scroll = Object.FindFirstObjectByType<ScrollRect>();

        sb.AppendLine("=== UI REPORT ===");

        // 1) Canvas + Scaler
        if (canvas)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            sb.AppendLine($"Canvas: {Path(canvas.transform)} scale={canvas.transform.lossyScale}");
            if (scaler)
            {
                sb.AppendLine($" CanvasScaler: mode={scaler.uiScaleMode} refRes={scaler.referenceResolution} match={scaler.matchWidthOrHeight} dppu={scaler.dynamicPixelsPerUnit}");
            }
            else sb.AppendLine(" CanvasScaler: <none>");
        }
        else sb.AppendLine("Canvas: <none found>");

        // 2) ScrollRect
        if (!scroll) { Debug.Log(sb.ToString() + "\nScrollRect: <none found>"); return; }

        sb.AppendLine($"\nScrollRect: {Path(scroll.transform)} horiz={scroll.horizontal} vert={scroll.vertical}");
        DumpRect(sb, " ScrollRect Rect", scroll.GetComponent<RectTransform>());
        var viewport = scroll.viewport;
        var content = scroll.content;

        // 3) Viewport
        if (viewport)
        {
            sb.AppendLine($"\nViewport: {Path(viewport)}");
            DumpRect(sb, " Viewport Rect", viewport);
            sb.AppendLine($"  Has Mask: {viewport.GetComponent<Mask>() != null}  Image: {viewport.GetComponent<Image>() != null}");
        }
        else sb.AppendLine("\nViewport: <none>");

        // 4) Content
        if (content)
        {
            sb.AppendLine($"\nContent: {Path(content)}");
            DumpRect(sb, " Content Rect", content);
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            var csf = content.GetComponent<ContentSizeFitter>();
            if (vlg)
            {
                sb.AppendLine($"  VLG: padding L{vlg.padding.left} R{vlg.padding.right} T{vlg.padding.top} B{vlg.padding.bottom}  spacing={vlg.spacing}");
                sb.AppendLine($"      ChildControl W={vlg.childControlWidth} H={vlg.childControlHeight}  ForceExpand W={vlg.childForceExpandWidth} H={vlg.childForceExpandHeight}");
                sb.AppendLine($"      Alignment={vlg.childAlignment}");
            }
            else sb.AppendLine("  VLG: <none>");
            if (csf) sb.AppendLine($"  CSF: H={csf.horizontalFit} V={csf.verticalFit}");
            else sb.AppendLine("  CSF: <none>");
        }
        else sb.AppendLine("\nContent: <none>");

        // 5) One OrderCard under Content (if any)
        if (content && content.childCount > 0)
        {
            var card = content.GetChild(0);
            sb.AppendLine($"\nOrderCard (first): {Path(card)} scale={card.lossyScale}");
            DumpRect(sb, " Card Rect", card.GetComponent<RectTransform>());

            var le = card.GetComponent<LayoutElement>();
            if (le)
                sb.AppendLine($"  LayoutElement: minH={le.minHeight} prefH={le.preferredHeight} flexH={le.flexibleHeight}");
            else sb.AppendLine("  LayoutElement: <none>");

            var csf = card.GetComponent<ContentSizeFitter>();
            if (csf) sb.AppendLine($"  CSF: H={csf.horizontalFit} V={csf.verticalFit}");
            else sb.AppendLine("  CSF: <none>");

            var mask = card.GetComponent<Mask>();
            sb.AppendLine($"  Mask on card: {(mask ? "YES" : "no")}");

            // 6) CardContent (child)
            if (card.childCount > 0)
            {
                var cardContent = card.GetChild(0);
                sb.AppendLine($"\n CardContent: {Path(cardContent)} scale={cardContent.lossyScale}");
                DumpRect(sb, "  CardContent Rect", cardContent.GetComponent<RectTransform>());

                var vlgC = cardContent.GetComponent<VerticalLayoutGroup>();
                var csfC = cardContent.GetComponent<ContentSizeFitter>();
                if (vlgC)
                {
                    sb.AppendLine($"   VLG: padding L{vlgC.padding.left} R{vlgC.padding.right} T{vlgC.padding.top} B{vlgC.padding.bottom}  spacing={vlgC.spacing}");
                    sb.AppendLine($"       ChildControl W={vlgC.childControlWidth} H={vlgC.childControlHeight}  ForceExpand W={vlgC.childForceExpandWidth} H={vlgC.childForceExpandHeight}");
                }
                else sb.AppendLine("   VLG: <none>");
                if (csfC) sb.AppendLine($"   CSF: H={csfC.horizontalFit} V={csfC.verticalFit}");
                else sb.AppendLine("   CSF: <none>");

                // 7) Texts
                var texts = cardContent.GetComponentsInChildren<Text>(true);
                foreach (var t in texts)
                {
                    sb.AppendLine($"   Text: {Path(t.transform)} size={t.fontSize} bestFit={t.resizeTextForBestFit} HOverflow={t.horizontalOverflow} VOverflow={t.verticalOverflow}");
                    DumpRect(sb, "    Text Rect", t.GetComponent<RectTransform>());
                }
            }
        }
        else sb.AppendLine("\nOrderCard: <none under Content>");

        // 8) Non-1 scale parents
        sb.AppendLine("\nParents with non-1 scale (from OrdersScroll up):");
        if (scroll)
        {
            var tr = scroll.transform;
            while (tr != null)
            {
                var s = tr.lossyScale;
                if (Vector3.Distance(s, Vector3.one) > 0.001f)
                    sb.AppendLine($" - {Path(tr)} scale={s}");
                tr = tr.parent;
            }
        }

        Debug.Log(sb.ToString());
    }

    static void DumpRect(StringBuilder sb, string label, RectTransform rt)
    {
        if (!rt) { sb.AppendLine($"{label}: <none>"); return; }
        sb.AppendLine($"{label}: anchorsMin={rt.anchorMin} anchorsMax={rt.anchorMax} pivot={rt.pivot} offs(LRTB)={rt.offsetMin.x},{rt.offsetMax.x},{rt.offsetMax.y},{rt.offsetMin.y} scale={rt.localScale}");
    }

    static string Path(Transform t)
    {
        var p = t.name;
        while (t.parent) { t = t.parent; p = t.name + "/" + p; }
        return p;
    }
}
#endif

