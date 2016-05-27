using System;
using Android.Views;

namespace SlidingLayerLib
{
	public abstract class LayerTransformer
	{

		/**
		 * This method is executed when the layer finish measurement. Use this method to set member values inside of
		 * the transformer so that they do not need to be calculated on every iteration of the animation;
		 *
		 * @param layerView       A reference to the layer itself.
		 * @param screenSide      Side of the screen where the layer is stuck to
		 */
		public virtual void OnMeasure (View layerView, int screenSide)
		{
		}

		/**
		 * Internal method to expose necessary properties for internal transformers to operate. Note that custom
		 * transformers will modify views based on their circumstances. For example,
		 * a default transformer if the view is stuck to the top or bottom, or if the preview is enabled,
		 * whereas a custom implementation has that information already.
		 *
		 * @param layerView       A reference to the layer itself.
		 * @param previewProgress The progress of the layer relative to the preview mode [0 - 1]. 0 fixed if no preview
		 * @param layerProgress   The progress of the layer relative to its total size [0 - 1]
		 * @param screenSide      Side of the screen where the layer is stuck to
		 */
		public virtual void InternalTransform (View layerView, float previewProgress, float layerProgress, int screenSide)
		{
			Transform (layerView, previewProgress, layerProgress);
		}

		/**
		 * Apply a property transformation to layer based on its scrolling state for the total size of the layer
		 * and preview mode.
		 *
		 * @param layerView       A reference to the layer itself.
		 * @param previewProgress The progress of the layer relative to the preview mode [0 - 1]. 0 fixed if no preview
		 * @param layerProgress   The progress of the layer relative to its total size [0 - 1]
		 */
		public abstract void Transform (View layerView, float previewProgress, float layerProgress);
	}
}

