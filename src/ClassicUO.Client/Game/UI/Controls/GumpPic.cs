// SPDX-License-Identifier: BSD-2-Clause

using System.Collections.Generic;
using ClassicUO.Input;
using ClassicUO.Network;
using ClassicUO.Renderer;
using ClassicUO.Utility;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.UI.Controls
{
    internal abstract class GumpPicBase : Control
    {
        private ushort _graphic;

        protected GumpPicBase()
        {
            CanMove = true;
            AcceptMouseInput = true;
        }

        // HINWEIS: 'virtual' hinzugefügt, damit abgeleitete Klassen (z.B. GumpPic) überschreiben können.
        public virtual ushort Graphic
        {
            get => _graphic;
            set
            {
                _graphic = value;

                ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(_graphic);

                if (gumpInfo.Texture == null)
                {
                    Dispose();
                    return;
                }

                Width = gumpInfo.UV.Width;
                Height = gumpInfo.UV.Height;
            }
        }

        public ushort Hue { get; set; }

        public override bool Contains(int x, int y)
        {
            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(_graphic);
            if (gumpInfo.Texture == null)
                return false;

            if (Client.Game.UO.Gumps.PixelCheck(Graphic, x - Offset.X, y - Offset.Y))
                return true;

            for (int i = 0; i < Children.Count; i++)
            {
                if (Children[i].Contains(x, y))
                    return true;
            }

            return false;
        }
    }

    internal class GumpPic : GumpPicBase
    {
        private int _baseWidth;
        private int _baseHeight;
        private float _scale = 1f;

        public GumpPic(int x, int y, ushort graphic, ushort hue)
        {
            X = x;
            Y = y;
            Graphic = graphic;
            Hue = hue;
            IsFromServer = true;
        }

        public GumpPic(List<string> parts)
            : this(
                int.Parse(parts[1]),
                int.Parse(parts[2]),
                UInt16Converter.Parse(parts[3]),
                (ushort)(
                    parts.Count > 4
                        ? TransformHue(
                            (ushort)(
                                UInt16Converter.Parse(parts[4].Substring(parts[4].IndexOf('=') + 1))
                                + 1
                            )
                        )
                        : 0
                )
            )
        { }

        public bool IsPartialHue { get; set; }
        public bool ContainsByBounds { get; set; }

        public float Scale
        {
            get => _scale;
            set
            {
                if (value <= 0) value = 1f;
                if (_scale != value)
                {
                    _scale = value;
                    ApplyScale();
                }
            }
        }

        public override ushort Graphic
        {
            get => base.Graphic;
            set
            {
                base.Graphic = value;
                // Speichere Basisgröße nach Setzen der Grafik
                _baseWidth = Width;
                _baseHeight = Height;
                ApplyScale();
            }
        }

        private void ApplyScale()
        {
            if (_baseWidth == 0 || _baseHeight == 0)
                return;

            Width = (int)(_baseWidth * _scale);
            Height = (int)(_baseHeight * _scale);
        }

        public override bool Contains(int x, int y)
        {
            if (ContainsByBounds)
            {
                return x >= 0 && y >= 0 && x < Width && y < Height;
            }

            if (_scale != 1f && _baseWidth > 0 && _baseHeight > 0)
            {
                // Auf unskalierte Textur-Koordinaten zurückrechnen
                int unscaledX = (int)(x / _scale);
                int unscaledY = (int)(y / _scale);

                ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(Graphic);
                if (gumpInfo.Texture == null)
                    return false;

                if (Client.Game.UO.Gumps.PixelCheck(Graphic, unscaledX - Offset.X, unscaledY - Offset.Y))
                    return true;

                // Kinder normal testen (sie bekommen bereits die skalierten x,y)
                for (int i = 0; i < Children.Count; i++)
                {
                    if (Children[i].Contains(x, y))
                        return true;
                }

                return false;
            }

            return base.Contains(x, y);
        }

        private static ushort TransformHue(ushort hue)
        {
            if (hue <= 2)
                hue = 0;
            return hue;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, IsPartialHue, Alpha, true);

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(Graphic);

            if (gumpInfo.Texture != null)
            {
                // Zeichnen direkt auf skalierte Zielrechteckgröße (Width/Height bereits skaliert)
                batcher.Draw(
                    gumpInfo.Texture,
                    new Rectangle(x, y, Width, Height),
                    gumpInfo.UV,
                    hueVector
                );
            }

            return base.Draw(batcher, x, y);
        }
    }

    internal class VirtueGumpPic : GumpPic
    {
        private readonly World _world;

        public VirtueGumpPic(World world, List<string> parts) : base(parts)
        {
            _world = world;
        }

        protected override bool OnMouseDoubleClick(int x, int y, MouseButtonType button)
        {
            if (button == MouseButtonType.Left)
            {
                NetClient.Socket.Send_VirtueGumpResponse(_world.Player, Graphic);
                return true;
            }

            return base.OnMouseDoubleClick(x, y, button);
        }
    }

    internal class GumpPicInPic : GumpPicBase
    {
        private readonly Rectangle _picInPicBounds;

        public GumpPicInPic(
            int x,
            int y,
            ushort graphic,
            ushort sx,
            ushort sy,
            ushort width,
            ushort height
        )
        {
            X = x;
            Y = y;
            Graphic = graphic;
            Width = width;
            Height = height;
            _picInPicBounds = new Rectangle(sx, sy, Width, Height);
            IsFromServer = true;
        }

        public GumpPicInPic(List<string> parts)
            : this(
                int.Parse(parts[1]),
                int.Parse(parts[2]),
                UInt16Converter.Parse(parts[3]),
                UInt16Converter.Parse(parts[4]),
                UInt16Converter.Parse(parts[5]),
                UInt16Converter.Parse(parts[6]),
                UInt16Converter.Parse(parts[7])
            )
        { }

        public override bool Contains(int x, int y)
        {
            return true;
        }

        public override bool Draw(UltimaBatcher2D batcher, int x, int y)
        {
            if (IsDisposed)
                return false;

            Vector3 hueVector = ShaderHueTranslator.GetHueVector(Hue, false, Alpha, true);

            ref readonly var gumpInfo = ref Client.Game.UO.Gumps.GetGump(Graphic);

            var sourceBounds = new Rectangle(gumpInfo.UV.X + _picInPicBounds.X, gumpInfo.UV.Y + _picInPicBounds.Y, _picInPicBounds.Width, _picInPicBounds.Height);

            if (gumpInfo.Texture != null)
            {
                batcher.Draw(
                    gumpInfo.Texture,
                    new Rectangle(x, y, Width, Height),
                    sourceBounds,
                    hueVector
                );
            }

            return base.Draw(batcher, x, y);
        }
    }
}
