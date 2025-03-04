using UnityEngine.UIElements;

public class MenuView
{
    private readonly VisualElement root;

    public MenuView(VisualElement root)
    {
        this.root = root;
    }

    public void SetVisible(bool visible)
    {
        root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
