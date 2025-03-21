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
        private Messages.Field myHoverSymbol = Messages.Field.Empty;

        public CellElement()
        {
            symbolClassTracker = new ClassTracker(this);
            hoverClassTracker = new ClassTracker(this);

            RegisterCallback<PointerEnterEvent>(OnHoverEnter);
            RegisterCallback<PointerLeaveEvent>(OnHoverExit);
        }

        public void SetMyHoverSymbol(Messages.Field symbol)
        {
            myHoverSymbol = symbol;
        }

        private void OnHoverEnter(PointerEnterEvent evt)
        {
            if (currentSymbol == Messages.Field.Empty)
            {
                hoverClassTracker.Set($"{HOVER}{myHoverSymbol.ToString().ToLower()}");
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