using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EchoDungeon.UI
{
    public sealed class RoomProgressUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI roomText;
        [SerializeField] private List<Image> roomNodes = new List<Image>();

        [Header("Colors")]
        [SerializeField] private Color completedColor = new Color(0.62f, 0.74f, 0.82f, 1f);
        [SerializeField] private Color currentColor = new Color(0.98f, 0.77f, 0.25f, 1f);
        [SerializeField] private Color lockedColor = new Color(0.22f, 0.24f, 0.29f, 0.88f);

        private Coroutine pulseRoutine;
        private int currentRoom;
        private int totalRooms;

        public void SetRoom(int current, int total)
        {
            currentRoom = Mathf.Max(0, current);
            totalRooms = Mathf.Max(0, total);

            if (roomText != null)
            {
                roomText.text = totalRooms > 0 ? "ROOM " + currentRoom + " / " + totalRooms : "ROOM --";
            }

            for (int i = 0; i < roomNodes.Count; i++)
            {
                Image node = roomNodes[i];
                if (node == null)
                {
                    continue;
                }

                bool visible = i < totalRooms;
                node.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                if (i + 1 < currentRoom)
                {
                    node.color = completedColor;
                }
                else if (i + 1 == currentRoom)
                {
                    node.color = currentColor;
                }
                else
                {
                    node.color = lockedColor;
                }
            }

            PulseCurrentNode();
        }

        private void PulseCurrentNode()
        {
            if (currentRoom <= 0 || currentRoom > roomNodes.Count)
            {
                return;
            }

            Image target = roomNodes[currentRoom - 1];
            if (target == null || !target.gameObject.activeInHierarchy)
            {
                return;
            }

            if (pulseRoutine != null)
            {
                StopCoroutine(pulseRoutine);
            }

            pulseRoutine = StartCoroutine(PulseRoutine(target.rectTransform));
        }

        private IEnumerator PulseRoutine(RectTransform target)
        {
            float timer = 0f;
            const float duration = 0.22f;
            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float scale = 1f + Mathf.Sin(timer / duration * Mathf.PI) * 0.25f;
                target.localScale = Vector3.one * scale;
                yield return null;
            }

            target.localScale = Vector3.one;
            pulseRoutine = null;
        }
    }
}
