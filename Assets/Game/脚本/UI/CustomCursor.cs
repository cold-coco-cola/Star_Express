using UnityEngine;

namespace Game.Scripts.UI
{
    public class CustomCursor : MonoBehaviour
    {
        public Texture2D cursorTexture;
        public Vector2 hotSpot = Vector2.zero;

        private void Start()
        {
            if (cursorTexture == null)
                cursorTexture = Resources.Load<Texture2D>("UI/cursor");
            if (cursorTexture != null)
            {
                Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.ForceSoftware);
                Cursor.visible = true;
            }
        }

        private void OnDestroy()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
