// Copyright (C) Josh Smith - January 2007

using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Hawk.Core.Utils
{
	/// <summary>
	/// Renders a visual which can follow the mouse cursor, 
	/// such as during a drag-and-drop operation.
	/// </summary>
	public class DragAdorner : Adorner
	{
		#region Data

		private Rectangle child = null;
		private double offsetLeft = 0;
		private double offsetTop = 0;

		#endregion // Data

		#region Constructor

		/// <summary>
		/// Initializes a new instance of DragVisualAdorner.
		/// </summary>
		/// <param name="adornedElement">The element being adorned.</param>
		/// <param name="size">The size of the adorner.</param>
		/// <param name="brush">A brush to with which to paint the adorner.</param>
		public DragAdorner( UIElement adornedElement, Size size, Brush brush )
			: base( adornedElement )
		{
			Rectangle rect = new Rectangle();
			rect.Fill = brush;
			rect.Width = size.Width;
			rect.Height = size.Height;
			rect.IsHitTestVisible = false;
			this.child = rect;
		}

		#endregion // Constructor

		#region Public Interface

		#region GetDesiredTransform

		/// <summary>
		/// Override.
		/// </summary>
		/// <param name="transform"></param>
		/// <returns></returns>
		public override GeneralTransform GetDesiredTransform( GeneralTransform transform )
		{
			GeneralTransformGroup result = new GeneralTransformGroup();
			result.Children.Add( base.GetDesiredTransform( transform ) );
			result.Children.Add( new TranslateTransform( this.offsetLeft, this.offsetTop ) );
			return result;
		}

		#endregion // GetDesiredTransform

		#region OffsetLeft

		/// <summary>
		/// Gets/sets the horizontal offset of the adorner.
		/// </summary>
		public double OffsetLeft
		{
			get { return this.offsetLeft; }
			set
			{
				this.offsetLeft = value;
				UpdateLocation();
			}
		}

		#endregion // OffsetLeft

		#region SetOffsets

		/// <summary>
		/// Updates the location of the adorner in one atomic operation.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="top"></param>
		public void SetOffsets( double left, double top )
		{
			this.offsetLeft = left;
			this.offsetTop = top;
			this.UpdateLocation();
		}

		#endregion // SetOffsets

		#region OffsetTop

		/// <summary>
		/// Gets/sets the vertical offset of the adorner.
		/// </summary>
		public double OffsetTop
		{
			get { return this.offsetTop; }
			set
			{
				this.offsetTop = value;
				UpdateLocation();
			}
		}

		#endregion // OffsetTop

		#endregion // Public Interface

		#region Protected Overrides

		/// <summary>
		/// Override.
		/// </summary>
		/// <param name="constraint"></param>
		/// <returns></returns>
		protected override Size MeasureOverride( Size constraint )
		{
			this.child.Measure( constraint );
			return this.child.DesiredSize;
		}

		/// <summary>
		/// Override.
		/// </summary>
		/// <param name="finalSize"></param>
		/// <returns></returns>
		protected override Size ArrangeOverride( Size finalSize )
		{
			this.child.Arrange( new Rect( finalSize ) );
			return finalSize;
		}

		/// <summary>
		/// Override.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		protected override Visual GetVisualChild( int index )
		{
			return this.child;
		}

		/// <summary>
		/// Override.  Always returns 1.
		/// </summary>
		protected override int VisualChildrenCount
		{
			get { return 1; }
		}

		#endregion // Protected Overrides

		#region Private Helpers

		private void UpdateLocation()
		{
			AdornerLayer adornerLayer = this.Parent as AdornerLayer;
			if( adornerLayer != null )
				adornerLayer.Update( this.AdornedElement );
		}

		#endregion // Private Helpers
	}
}