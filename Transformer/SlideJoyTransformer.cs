using System;
using Android.Animation;
using Android.Views;

namespace SlidingLayerLib
{
	public class SlideJoyTransformer : LayerTransformer
	{


		private float [] mCuePoints = new float [] { 0.7f, 0.9f, 1 };

		private float [] mRotationXValues;
		private float [] mRotationYValues;

		public override void OnMeasure (Android.Views.View layerView, int screenSide)
		{
			// Rotation
			float [] rotationXY = rotationValueForScreenSide (-4.75f, screenSide);

			mRotationXValues = new float [] { 0, rotationXY [0], 0 };
			mRotationYValues = new float [] { 0, rotationXY [1], 0 };

			// Pivot
			int [] pivotPosition = pivotPositionForScreenSide (layerView, screenSide);
			layerView.PivotX = pivotPosition [0];
			layerView.PivotY = pivotPosition [1];
		}

		public override void Transform (Android.Views.View layerView, float previewProgress, float layerProgress)
		{
			throw new NotImplementedException ();
		}

		public override void InternalTransform (Android.Views.View layerView, float previewProgress, float layerProgress, int screenSide)
		{
			float progressRatioToAnimate = Math.Max (previewProgress, layerProgress);

			// Scale
			float scaleValue = Transitions.intermediateValueForCuePoints (progressRatioToAnimate,
					new float [] { 0.9f, 1 });


			layerView.ScaleX = scaleValue;
			layerView.ScaleY = scaleValue;

			// Rotation
			float rotationX, rotationY;

			rotationX = Transitions.intermediateValueForRange (progressRatioToAnimate, mCuePoints, mRotationXValues);
			layerView.RotationX = rotationX;

			rotationY = Transitions.intermediateValueForRange (progressRatioToAnimate, mCuePoints, mRotationYValues);
			layerView.RotationY = rotationY;
		}

		private float [] rotationValueForScreenSide (float value, int screenSide)
		{

			switch (screenSide) {

			case SlidingLayer.STICK_TO_LEFT:
				return new float [] { 0, value };

			case SlidingLayer.STICK_TO_TOP:
				return new float [] { -value, 0 };

			case SlidingLayer.STICK_TO_RIGHT:
				return new float [] { 0, -value };

			case SlidingLayer.STICK_TO_BOTTOM:
				return new float [] { value, 0 };

			default:
				return new float [] { 0, 0 };
			}
		}

		private int [] pivotPositionForScreenSide (View layerView, int screenSide)
		{

			switch (screenSide) {

			case SlidingLayer.STICK_TO_LEFT:
				return new int [] { 0, layerView.MeasuredHeight / 2 };

			case SlidingLayer.STICK_TO_TOP:
				return new int [] { layerView.MeasuredWidth / 2, 0 };

			case SlidingLayer.STICK_TO_RIGHT:
				return new int [] { layerView.MeasuredWidth, layerView.MeasuredHeight / 2 };

			case SlidingLayer.STICK_TO_BOTTOM:
				return new int [] { layerView.MeasuredWidth / 2, layerView.MeasuredHeight };

			default:
				return new int [] { 0, 0 };
			}
		}
	}
}

