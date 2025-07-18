using AvalonInjectLib.Graphics;
using static AvalonInjectLib.Structs;

namespace AvalonInjectLib.UIFramework
{
    public class Image : UIControl
    {
        // Constantes por defecto
        private const float DEFAULT_WIDTH = 64f;
        private const float DEFAULT_HEIGHT = 64f;

        // Textura de la imagen
        private Texture2D _texture;
        public Texture2D Texture
        {
            get => _texture;
            set
            {
                _texture = value;
                if (_texture != null && MaintainAspectRatio)
                {
                    CalculateAspectRatioSize();
                }
            }
        }

        // Propiedades de la imagen
        public bool MaintainAspectRatio { get; set; } = true;
        public Color TintColor { get; set; } = Color.White;
        public bool IsInteractive { get; set; } = false;

        // Eventos (solo si es interactivo)
        public event Action Click;

        // Estados (solo si es interactivo)
        public bool IsHovered { get; private set; } = false;

        // Constructor
        public Image()
        {
            Width = DEFAULT_WIDTH;
            Height = DEFAULT_HEIGHT;
        }

        public Image(Texture2D texture) : this()
        {
            Texture = texture;
        }

        public override void Draw()
        {
            if (!Visible || _texture == null) return;

            var absPos = GetAbsolutePosition();
            var drawColor = Enabled ? TintColor : Color.FromArgb(150, 150, 150);

            Renderer.DrawTexture(_texture, new Rectangle((int)absPos.X, (int)absPos.Y, (int)Width, (int)Height), drawColor);

            // Dibujar borde si es interactivo y está hover
            if (IsInteractive && IsHovered)
            {
                Renderer.DrawRectOutline(new Rect(absPos.X, absPos.Y, Width, Height),
                                       Color.FromArgb(100, 100, 255), 2f);
            }
        }

        protected override void OnMouseMove(object sender, Vector2 pos)
        {
            base.OnMouseMove(sender, pos);
            if (IsInteractive)
            {
                var mousePos = UIEventSystem.MousePosition;
                IsHovered = Contains(mousePos);
            }
        }

        protected override void OnClick(object sender, Vector2 pos)
        {
            base.OnClick(sender, pos);
            var mousePos = UIEventSystem.MousePosition;
            if (IsInteractive && Contains(mousePos) && Enabled)
            {
                Click?.Invoke();
            }
        }

        // Métodos privados
        private void CalculateAspectRatioSize()
        {
            if (_texture == null) return;

            float aspectRatio = (float)_texture.Width / _texture.Height;
            Height = Width / aspectRatio;
        }

        // Métodos públicos
        public void SetSizeFromTexture()
        {
            if (_texture != null)
            {
                Width = _texture.Width;
                Height = _texture.Height;
            }
        }
    }
}