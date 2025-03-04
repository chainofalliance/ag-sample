public class MenuController
{
    private readonly MenuView view;

    public MenuController(MenuView view)
    {
        this.view = view;
    }

    public void SetVisible(bool visible)
    {
        view.SetVisible(visible);
    }
}
