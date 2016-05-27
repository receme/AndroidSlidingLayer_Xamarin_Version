/*
 * SlidingLayer.java
 *
 * Copyright (C) 2015 6 Wunderkinder GmbH.
 *
 * @author      Jose L Ugia - @Jl_Ugia
 * @author      Antonio Consuegra - @aconsuegra
 * @author      Cesar Valiente - @CesarValiente
 * @version     1.2.0
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Android.Views;

namespace SlidingLayerLib
{
	public class RotationTransformer : LayerTransformer
	{


		private static int DEFAULT_ANGLE = 10;

		private float mMaxAngle;
		private float mAngle;

		public RotationTransformer ()
		{
			//this (DEFAULT_ANGLE);
		}

		public RotationTransformer (float maxAngle)
		{
			mMaxAngle = maxAngle;
		}

		public override void OnMeasure (View layerView, int screenSide)
		{
			int [] pivotPosition = pivotPositionForScreenSide (layerView, screenSide);
			layerView.PivotX = pivotPosition [0];
			layerView.PivotY = pivotPosition [1];


			mAngle = mMaxAngle *
					(screenSide == SlidingLayer.STICK_TO_LEFT || screenSide == SlidingLayer.STICK_TO_TOP ? -1 : 1);
		}

		public override void Transform (View layerView, float previewProgress, float layerProgress)
		{

		}

		public override void InternalTransform (View layerView, float previewProgress, float layerProgress, int screenSide)
		{
			float progressRatioToAnimate = Java.Lang.Math.Max (previewProgress, layerProgress);
			layerView.Rotation = mAngle * (1 - progressRatioToAnimate);
		}


		private int [] pivotPositionForScreenSide (View layerView, int screenSide)
		{

			switch (screenSide) {

			case SlidingLayer.STICK_TO_LEFT:
				return new int [] { 0, layerView.MeasuredHeight };

			case SlidingLayer.STICK_TO_TOP:
				return new int [] { 0, 0 };

			case SlidingLayer.STICK_TO_RIGHT:
				return new int [] { layerView.MeasuredWidth, layerView.MeasuredHeight };

			case SlidingLayer.STICK_TO_BOTTOM:
				return new int [] { 0, layerView.MeasuredHeight };

			default:
				return new int [] { 0, 0 };
			}
		}
	}
}


