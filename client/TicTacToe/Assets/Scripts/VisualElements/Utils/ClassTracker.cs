using System;
using UnityEngine.UIElements;

public class ClassTracker : IDisposable
{
    private readonly VisualElement visualElement;
    private string currentClass = null;

    public ClassTracker(VisualElement visualElement, params string[] possibleAppliedClasses)
    {
        this.visualElement = visualElement;

        foreach (var possibleClass in possibleAppliedClasses)
        {
            if (visualElement.ClassListContains(possibleClass))
            {
                currentClass = possibleClass;
                break;
            }
        }
    }

    public void Set(string newClass)
    {
        visualElement?.RemoveFromClassList(currentClass);
        currentClass = newClass;
        visualElement?.AddToClassList(currentClass);
    }

    public void Dispose()
    {
        visualElement?.RemoveFromClassList(currentClass);
    }
}
