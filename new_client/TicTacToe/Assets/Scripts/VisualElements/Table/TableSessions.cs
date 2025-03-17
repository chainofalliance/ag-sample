using System.Collections.Generic;
using UnityEngine.UIElements;
using System;

using static Queries;

namespace TTT.Components
{
    [UxmlElement]
    partial class TableSessions : VisualElement
    {
        public event Action OnClickViewAllSessions;

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

            viewAllButton.clicked += () => OnClickViewAllSessions?.Invoke();
        }

        public void Populate(List<PlayerHistoryResponse> historyEntries)
        {
            foreach (TableSessionsEntry entry in entries)
            {
                entry.SetVisible(false);
            }

            for (int i = 0; i < historyEntries.Count; i++)
            {
                var e = entries[i];
                e.Populate(historyEntries[i]);
                e.SetVisible(true);
            }
        }
    }
}