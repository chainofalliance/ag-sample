using UnityEngine.UIElements;
using System;

public class MenuView
{
    public event Action OnPlayPve;

    private readonly VisualElement root;
    private readonly Button playPveButton;

    public MenuView(VisualElement root)
    {
        this.root = root;

        playPveButton = root.Q<Button>("PlayPveButton");
        playPveButton.clicked += () => OnPlayPve?.Invoke();
    }

    public void SetVisible(bool visible)
    {
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
