using UnityEngine;
using UnityEngine.UI;

namespace UI.Responsive {
    public enum GridAdjustmentMode {
        Horizontal,
        Vertical
    }

    [RequireComponent(typeof(GridLayoutGroup))]
    public class UIGridCellSizeResponsive : MonoBehaviour {
        [SerializeField] private Sprite sprite; 
        [SerializeField] private GridAdjustmentMode mode = GridAdjustmentMode.Horizontal;
        [SerializeField] private int numElements = 1; 
        [SerializeField] private Vector2 spacing = Vector2.zero;

        private GridLayoutGroup gridLayoutGroup;
        private RectTransform rectTransform;

        private void Awake() {
            gridLayoutGroup = GetComponent<GridLayoutGroup>();
            rectTransform = GetComponent<RectTransform>();
        }

        private void Start() {
            AdjustCellSize();
        }

        public void AdjustCellSize() {
            if (sprite == null) {
                Debug.LogError("Sprite is not assigned!");
                return;
            }

            if (numElements <= 0) {
                Debug.LogError("numElements must be greater than 0!");
                return;
            }

            // Calculate aspect ratio: width / height of the sprite
            float aspectRatio = sprite.rect.width / sprite.rect.height;

            // Get the current rect size
            Vector2 rectSize = rectTransform.rect.size;

            Vector2 cellSize = Vector2.zero;

            if (mode == GridAdjustmentMode.Horizontal) {
                // Set constraint to fixed columns
                gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayoutGroup.constraintCount = numElements;

                // Calculate cell width: (total width - (columns-1)*spacing.x) / columns
                float totalSpacingX = (numElements - 1) * spacing.x;
                cellSize.x = (rectSize.x - totalSpacingX) / numElements;

                // Calculate cell height based on aspect ratio: height = width / aspectRatio
                cellSize.y = cellSize.x / aspectRatio;
            }
            else if (mode == GridAdjustmentMode.Vertical) {
                // Set constraint to fixed rows
                gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedRowCount;
                gridLayoutGroup.constraintCount = numElements;

                // Calculate cell height: (total height - (rows-1)*spacing.y) / rows
                float totalSpacingY = (numElements - 1) * spacing.y;
                cellSize.y = (rectSize.y - totalSpacingY) / numElements;

                // Calculate cell width based on aspect ratio: width = height * aspectRatio
                cellSize.x = cellSize.y * aspectRatio;
            }

            // Set the cell size
            gridLayoutGroup.cellSize = cellSize;
            gridLayoutGroup.spacing = spacing;

            // Optional: Force layout rebuild if needed
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }
}
