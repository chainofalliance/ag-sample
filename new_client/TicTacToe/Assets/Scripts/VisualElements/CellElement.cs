using System;
using UnityEngine.UIElements;

namespace TTT.Components
{
    [UxmlElement]
    partial class CellElement : VisualElement
    {
        const string SYMBOL = "symbol-";
        const string HOVER = "hover-";

        private readonly ClassTracker symbolClassTracker;
        private readonly ClassTracker hoverClassTracker;

        private Messages.Field currentSymbol = Messages.Field.Empty;

        public CellElement()
        {
            symbolClassTracker = new ClassTracker(this);
            hoverClassTracker = new ClassTracker(this);

            RegisterCallback<PointerEnterEvent>(OnHoverEnter);
            RegisterCallback<PointerLeaveEvent>(OnHoverExit);
        }

        private void OnHoverEnter(PointerEnterEvent evt)
        {
            if (currentSymbol == Messages.Field.Empty)
            {
                hoverClassTracker.Set($"{HOVER}x");
            }
        }

        private void OnHoverExit(PointerLeaveEvent evt)
        {
            hoverClassTracker.Set("");
        }

        public void SetSymbol(Messages.Field symbol)
        {
            currentSymbol = symbol;
            symbolClassTracker.Set($"{SYMBOL}{symbol.ToString().ToLower()}");
        }
    }
}