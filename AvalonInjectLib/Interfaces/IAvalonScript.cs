using AvalonInjectLib.Scripting;

namespace AvalonInjectLib.Interfaces
{
    public interface IAvalonScript
    {  
        /// <summary>
       /// Type of the control
       /// </summary>
        ScriptControlType Type { get; }

        string Category { get; }
        /// <summary>
        /// Nombre identificador del script
        /// </summary>
        string Name { get; }

        bool IsEnabled { get; set; }

        /// <summary>
        /// Inicializa el script con configuración
        /// </summary>
        void Initialize();

        /// <summary>
        /// Numeric value (for Slider)
        /// </summary>
        object Value { get; set; }

        /// <summary>
        /// Ejecuta la Update
        /// </summary>
        void Update();


        /// <summary>
        /// Ejecuta la Update
        /// </summary>
        void Draw();

        /// <summary>
        /// Ejecuta el cambio de valor
        /// </summary>
        void ChangeValue(object value);
    }
}
