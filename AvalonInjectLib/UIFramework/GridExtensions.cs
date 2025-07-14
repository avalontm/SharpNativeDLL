// OPCIÓN 1: Extensión de la clase Grid con métodos más expresivos
using AvalonInjectLib.UIFramework;

public static class GridExtensions
{
    /// <summary>
    /// Agrega un control en una posición específica del grid
    /// </summary>
    public static void AddAt(this Grid grid, UIControl control, int column, int row, int columnSpan = 1, int rowSpan = 1)
    {
        control.SetGridPosition(column, row, columnSpan, rowSpan);
        grid.AddChild(control);
    }

    /// <summary>
    /// Agrega un control que ocupa una sola celda
    /// </summary>
    public static void AddCell(this Grid grid, UIControl control, int column, int row)
    {
        grid.AddAt(control, column, row, 1, 1);
    }

    /// <summary>
    /// Agrega un control que se extiende horizontalmente
    /// </summary>
    public static void AddRow(this Grid grid, UIControl control, int row, int columnSpan = 1, int startColumn = 0)
    {
        grid.AddAt(control, startColumn, row, columnSpan, 1);
    }

    /// <summary>
    /// Agrega un control que se extiende verticalmente
    /// </summary>
    public static void AddColumn(this Grid grid, UIControl control, int column, int rowSpan = 1, int startRow = 0)
    {
        grid.AddAt(control, column, startRow, 1, rowSpan);
    }

    /// <summary>
    /// Agrega un control que ocupa múltiples celdas
    /// </summary>
    public static void AddSpan(this Grid grid, UIControl control, int column, int row, int columnSpan, int rowSpan)
    {
        grid.AddAt(control, column, row, columnSpan, rowSpan);
    }
}