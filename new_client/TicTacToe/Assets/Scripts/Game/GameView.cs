using UnityEngine.UIElements;

public class GameView
{
    private readonly VisualElement root;

    public GameView(VisualElement root)
    {
        this.root = root;
    }

    public void SetVisible(bool visible)
    {
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
