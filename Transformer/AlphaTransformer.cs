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
namespace SlidingLayerLib
{
	public class AlphaTransformer : LayerTransformer
	{


		private static int DEFAULT_MULTIPLIER = 1;

		private float mMultiplier;

		public AlphaTransformer ()
		{
			//this (DEFAULT_MULTIPLIER);
		}

		public AlphaTransformer (float multiplier)
		{
			mMultiplier = multiplier;
		}

		public override void Transform (Android.Views.View layerView, float previewProgress, float layerProgress)
		{
			float progressRatioToAnimate = Java.Lang.Math.Max (previewProgress, layerProgress);
			float alpha = Java.Lang.Math.Max (0, Java.Lang.Math.Min (1, progressRatioToAnimate * mMultiplier));
			layerView.Alpha = alpha;
		}

	}

}
