namespace FellsideDigital.Web;

/// <summary>
/// Index-safe move/remove operations shared by the in-place list editors (project plan
/// phases, hero metrics, pipeline steps, integrations) so the bounds-checking isn't
/// re-implemented per editor.
/// </summary>
public static class ListEditing
{
    public static void RemoveAt<T>(List<T> list, int index)
    {
        if (index >= 0 && index < list.Count)
            list.RemoveAt(index);
    }

    public static void MoveUp<T>(List<T> list, int index)
    {
        if (index > 0 && index < list.Count)
            (list[index - 1], list[index]) = (list[index], list[index - 1]);
    }

    public static void MoveDown<T>(List<T> list, int index)
    {
        if (index >= 0 && index < list.Count - 1)
            (list[index], list[index + 1]) = (list[index + 1], list[index]);
    }
}
