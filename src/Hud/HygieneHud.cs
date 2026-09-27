using System;
using System.Collections;
using System.Linq;
using Assets.Scripts;
using Assets.Scripts.Objects.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SuitEnhancementSuite;

/// <summary>
/// Shows the local player's hygiene in the Life Functions panel by enabling the vanilla PanelHygiene row (hidden until
/// the vanilla alert threshold) and the vanilla PanelWaste row below it. The waste value stays owned by the vanilla
/// updater. Older HUD layouts without those rows get a cloned row instead. Nothing runs in batch mode.
/// </summary>
internal sealed class HygieneHud
{
    private const string RowLabel = "Toaleta / hygiena";
    private const float SearchIntervalSeconds = 10f;

    private TMP_Text _value;
    private Text _legacyValue;
    private float _nextSearchTime;
    private int _lastPercent = -1;

    public IEnumerator Run()
    {
        if (GameManager.IsBatchMode) yield break;
        var wait = new WaitForSecondsRealtime(1f);
        while (true)
        {
            try
            {
                if (_value == null) TryAttach();
                var human = Human.LocalHuman;
                if (human != null) Show(Mathf.RoundToInt(human.HygieneRatio * 100f));
            }
            catch (Exception e) { Plugin.Log?.LogDebug($"Native sanitation row unavailable: {e.Message}"); }
            yield return wait;
        }
    }

    private void Show(int percent)
    {
        if (percent == _lastPercent) return;
        _lastPercent = percent;
        if (_value != null) _value.text = $"{percent}";
        if (_legacyValue != null) _legacyValue.text = $"{percent}";
    }

    // Runs at most every SearchIntervalSeconds until a value text is attached. FindObjectsOfType is required: the
    // vanilla HUD is a scene object, not an asset in Resources.
    private void TryAttach()
    {
        if (Time.unscaledTime < _nextSearchTime) return;
        _nextSearchTime = Time.unscaledTime + SearchIntervalSeconds;
        if (TryEnableVanillaRows()) return;

        var all = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
        var hidden = all.FirstOrDefault(t => t != null &&
            (t.text.IndexOf("sanitation", StringComparison.OrdinalIgnoreCase) >= 0 ||
             t.text.IndexOf("hygiene", StringComparison.OrdinalIgnoreCase) >= 0 ||
             t.text.IndexOf("toaleta", StringComparison.OrdinalIgnoreCase) >= 0));
        if (hidden != null)
        {
            hidden.gameObject.SetActive(true);
            _value = hidden.transform.parent.GetComponentsInChildren<TMP_Text>(true)
                .FirstOrDefault(t => t != hidden && t.text.Contains("%"));
            if (_value != null) return;
            if (!TryCloneLegacyRow()) return;
        }
        CloneThirstRow(all);
    }

    private bool TryEnableVanillaRows()
    {
        var hygieneValue = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true)
            .FirstOrDefault(t => t != null && t.name == "ValueHygiene");
        if (hygieneValue == null) return false;
        var panel = FindAncestor(hygieneValue.transform, "PanelHygiene");
        if (panel == null) return false;

        panel.gameObject.SetActive(true);
        _value = hygieneValue;
        _value.text = $"{Mathf.RoundToInt((Human.LocalHuman != null ? Human.LocalHuman.HygieneRatio : 0f) * 100f)}";
        var label = panel.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t != hygieneValue &&
            (t.name.IndexOf("Title", StringComparison.OrdinalIgnoreCase) >= 0 ||
             t.name.IndexOf("Label", StringComparison.OrdinalIgnoreCase) >= 0));
        if (label != null) label.text = RowLabel;
        Plugin.Log.LogInfo("[Suit Enhancement Suite] Activated vanilla PanelHygiene row.");
        EnableWasteRowBelow(panel);
        return true;
    }

    // PanelWaste sits at the same anchor as the hidden hygiene row in the vanilla prefab. Placing it directly after
    // hygiene gives it its own line in the parent's layout group.
    private static void EnableWasteRowBelow(Transform hygienePanel)
    {
        var wasteValue = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true)
            .FirstOrDefault(t => t != null && t.name == "ValueWaste");
        if (wasteValue == null) return;
        var wastePanel = FindAncestor(wasteValue.transform, "PanelWaste");
        if (wastePanel == null) return;

        wastePanel.gameObject.SetActive(true);
        wastePanel.SetSiblingIndex(hygienePanel.GetSiblingIndex() + 1);
        wastePanel.localScale = Vector3.one;
        if (hygienePanel is RectTransform hygieneRect && wastePanel is RectTransform wasteRect)
        {
            wasteRect.anchorMin = hygieneRect.anchorMin;
            wasteRect.anchorMax = hygieneRect.anchorMax;
            wasteRect.pivot = hygieneRect.pivot;
            wasteRect.sizeDelta = hygieneRect.sizeDelta;
            wasteRect.anchoredPosition = hygieneRect.anchoredPosition -
                new Vector2(0f, Mathf.Max(28f, hygieneRect.rect.height + 2f));
        }
        foreach (var graphic in wastePanel.GetComponentsInChildren<Graphic>(true))
            graphic.enabled = true;
        if (wastePanel.parent is RectTransform parentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        Plugin.Log.LogInfo("[Suit Enhancement Suite] Activated vanilla PanelWaste row for toilet need.");
    }

    private bool TryCloneLegacyRow()
    {
        var thirstLabel = UnityEngine.Object.FindObjectsOfType<Text>(true).FirstOrDefault(t => t != null &&
            (t.text.IndexOf("thirst", StringComparison.OrdinalIgnoreCase) >= 0 ||
             t.text.IndexOf("žízeň", StringComparison.OrdinalIgnoreCase) >= 0 ||
             t.text.IndexOf("hydrat", StringComparison.OrdinalIgnoreCase) >= 0));
        if (thirstLabel == null) return false;
        var row = thirstLabel.transform.parent.gameObject;
        var clone = UnityEngine.Object.Instantiate(row, row.transform.parent);
        clone.name = "SuitEnhancementSuite_SanitationRow";
        clone.transform.SetSiblingIndex(row.transform.GetSiblingIndex() + 1);
        var texts = clone.GetComponentsInChildren<Text>(true);
        var label = texts.FirstOrDefault(t => t != null && t.text != thirstLabel.text);
        _legacyValue = texts.LastOrDefault();
        if (label != null) label.text = RowLabel;
        return true;
    }

    private void CloneThirstRow(TMP_Text[] all)
    {
        var thirst = all.FirstOrDefault(t => t != null &&
            (t.text.IndexOf("thirst", StringComparison.OrdinalIgnoreCase) >= 0 ||
             t.text.IndexOf("žízeň", StringComparison.OrdinalIgnoreCase) >= 0));
        if (thirst == null) return;
        var row = thirst.transform.parent.gameObject;
        var clone = UnityEngine.Object.Instantiate(row, row.transform.parent);
        clone.name = "SuitEnhancementSuite_SanitationRow";
        clone.transform.SetSiblingIndex(row.transform.GetSiblingIndex() + 1);
        var label = clone.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(t => t != null && t != thirst);
        _value = clone.GetComponentsInChildren<TMP_Text>(true).LastOrDefault();
        if (label != null) label.text = RowLabel;
    }

    private static Transform FindAncestor(Transform start, string name)
    {
        var current = start;
        while (current != null && current.name != name) current = current.parent;
        return current;
    }
}
