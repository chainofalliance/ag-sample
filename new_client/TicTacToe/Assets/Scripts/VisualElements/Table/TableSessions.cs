using System.Collections.Generic;
using UnityEngine.UIElements;

namespace TTT.Components
{
    [UxmlElement]
    partial class TableSessions : VisualElement
    {
        private List<TableSessionsEntry> entries;
        private Button viewAllButton;

        public TableSessions()
        {
            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            entries = this.Query<TableSessionsEntry>().ToList();
            viewAllButton = this.Q<Button>("ButtonViewAll");
        }

        public void Populate()
        {

        }
    }
}