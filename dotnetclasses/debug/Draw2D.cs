
using Godot;
using System;
using System.Collections.Generic;
namespace DebugDraw;
public partial class Draw2D : Node2D
{
	private class DebugPoint
	{
		public Vector2 Position;
		public Color Color;
		public float Radius;
		public double ExpiresAt;

		public DebugPoint(Vector2 position, Color color, float radius, double expiresAt)
		{
			Position = position;
			Color = color;
			Radius = radius;
			ExpiresAt = expiresAt;
		}
	}

	private class DebugLine
	{
		public Vector2 From;
		public Vector2 To;
		public Color Color;
		public float Width;
		public double ExpiresAt;

		public DebugLine(Vector2 from,Vector2 to,Color color,float width,double expiresAt)
		{
			From = from;
			To = to;
			Color = color;
			Width = width;
			ExpiresAt = expiresAt;
		}
	}

	private class DebugText
	{
		public Vector2 Position;
		public string Text;
		public Color Color;
		public double ExpiresAt;

		public DebugText(Vector2 position,string text,Color color,double expiresAt)
		{
			Position = position;
			Text = text;
			Color = color;
			ExpiresAt = expiresAt;
		}
	}

	private readonly List<DebugPoint> _points = new();
	private readonly List<DebugLine> _lines = new();
	private readonly List<DebugText> _texts = new();

	private Font _font;

	public override void _Ready()
	{
		_font = ThemeDB.FallbackFont;
	}

	public override void _Process(double delta)
	{
		double now = Time.GetTicksMsec() / 1000.0;

		_points.RemoveAll(p => p.ExpiresAt <= now);
		_lines.RemoveAll(l => l.ExpiresAt <= now);
		_texts.RemoveAll(t => t.ExpiresAt <= now);

		QueueRedraw();
	}

	public override void _Draw()
	{
		foreach (DebugPoint p in _points)
		{
			DrawCircle(p.Position, p.Radius, p.Color);
		}

		foreach (DebugLine l in _lines)
		{
			DrawLine(l.From, l.To, l.Color, l.Width);
		}

		foreach (DebugText t in _texts)
		{
			DrawString(
				_font,
				t.Position,
				t.Text,
				HorizontalAlignment.Left,
				-1,
				8,
				t.Color
			);
		}
	}


	public void Point(Vector2 position,float duration = 1.0f,Color? color = null,float radius = 4.0f)
	{
		double now = Time.GetTicksMsec() / 1000.0;

		_points.Add(new DebugPoint(
			position,
			color ?? Colors.Red,
			radius,
			now + duration
		));

		QueueRedraw();
	}


	public void Line(Vector2 from,Vector2 to,float duration = 1.0f,Color? color = null,float width = 2.0f)
	{
		double now = Time.GetTicksMsec() / 1000.0;

		_lines.Add(new DebugLine(
			from,
			to,
			color ?? Colors.Green,
			width,
			now + duration
		));

		QueueRedraw();
	}


	public void Path(IEnumerable<Vector2> points,float duration = 1.0f,Color? color = null,float width = 2.0f)
	{
		double now = Time.GetTicksMsec() / 1000.0;
		Color actualColor = color ?? Colors.Green;

		Vector2? previous = null;

		foreach (Vector2 point in points)
		{
			if (previous.HasValue)
			{
				_lines.Add(new DebugLine(
					previous.Value,
					point,
					actualColor,
					width,
					now + duration
				));
			}

			previous = point;
		}

		QueueRedraw();
	}


	public void Text(Vector2 position,Variant value,float duration = 1.0f,Color? color = null)
	{
		double now = Time.GetTicksMsec() / 1000.0;

		_texts.Add(new DebugText(
			position,
			value.ToString(),
			color ?? Colors.White,
			now + duration
		));

		QueueRedraw();
	}
}
