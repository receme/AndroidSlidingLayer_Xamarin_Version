# AndroidSlidingLayer Xamarin.android version


This is the xamarin porting of the library https://github.com/wunderlist/android-sliding-layer-lib

Thanks to 'wunderlist'

you can download from nuget: https://www.nuget.org/packages/Android-SlidingLayerLib


**How to use:**

In your layout XML:

```xml
<SlidingLayerLib.SlidingLayer
        android:id="@+id/slidingLayer1"
        android:layout_width="match_parent"
        android:layout_height="wrap_content"
        android:layout_below="@id/monthNameTv"
        android:layout_above="@+id/bottomPanel">
        
        <!--the layout component you want to slide -->
                       
    </SlidingLayerLib.SlidingLayer>
```




SlidingLayerManager.cs

```C#
public class SlidingLayerManager
	{
		Context context;
		SlidingLayer mSlidingLayer;

		public SlidingLayerManager (Context _context, SlidingLayer _slidingLayer)
		{
			context = _context;
			mSlidingLayer = _slidingLayer;
		}

		public void setupSlidingLayerPosition (string layerPosition)
		{

			RelativeLayout.LayoutParams rlp = (RelativeLayout.LayoutParams)mSlidingLayer.LayoutParameters;
			int textResource;
			Drawable d;

			switch (layerPosition) {
			case "right":
				textResource = Resource.String.swipe_right_label;
				d = context.Resources.GetDrawable (Resource.Mipmap.Icon);
				mSlidingLayer.setStickTo (SlidingLayer.STICK_TO_RIGHT);
				break;
			case "left":
				textResource = Resource.String.swipe_left_label;
				d = context.Resources.GetDrawable (Resource.Mipmap.Icon);

				mSlidingLayer.setStickTo (SlidingLayer.STICK_TO_LEFT);
				break;
			case "top":
				textResource = Resource.String.swipe_up_label;
				d = context.Resources.GetDrawable (Resource.Mipmap.Icon);
				mSlidingLayer.setStickTo (SlidingLayer.STICK_TO_TOP);
				rlp.Width = RelativeLayout.LayoutParams.MatchParent;
				rlp.Height = context.Resources.GetDimensionPixelSize (Resource.Dimension.layer_size);
				break;
			case "bottom":
				textResource = Resource.String.swipe_down_label;
				d = context.Resources.GetDrawable (Resource.Mipmap.Icon);
				mSlidingLayer.setStickTo (SlidingLayer.STICK_TO_BOTTOM);
				rlp.Width = RelativeLayout.LayoutParams.MatchParent;
				rlp.Height = context.Resources.GetDimensionPixelSize (Resource.Dimension.layer_size);
				break;
			}

			//d.SetBounds (0, 0, d.IntrinsicWidth, d.IntrinsicHeight);
			//swipeText.setCompoundDrawables (null, d, null, null);
			//swipeText.setText (getResources ().getString (textResource));
			mSlidingLayer.LayoutParameters = rlp;
		}

		public void setupSlidingLayerTransform (String layerTransform)
		{

			LayerTransformer transformer;

			switch (layerTransform) {
			case "alpha":
				transformer = new AlphaTransformer ();
				break;
			case "rotation":
				transformer = new RotationTransformer ();
				break;
			case "slide":
				transformer = new SlideJoyTransformer ();
				break;
			default:
				return;
			}
			mSlidingLayer.setLayerTransformer (transformer);
		}

		public void setupShadow (bool enabled)
		{
			if (enabled) {
				mSlidingLayer.setShadowSizeRes (Resource.Dimension.shadow_size);
				mSlidingLayer.setShadowDrawable (Resource.Drawable.sidebar_shadow);
			} else {
				mSlidingLayer.setShadowSize (0);
				mSlidingLayer.setShadowDrawable (null);
			}
		}

		public void setupLayerOffset (bool enabled)
		{
			int offsetDistance = enabled ? context.Resources.GetDimensionPixelOffset (Resource.Dimension.offset_distance) : 0;
			mSlidingLayer.setOffsetDistance (offsetDistance);
		}

		public void setupPreviewMode (bool enabled)
		{
			int previewOffset = enabled ? context.Resources.GetDimensionPixelOffset (Resource.Dimension.preview_offset_distance) : -1;
			mSlidingLayer.setPreviewOffsetDistance (previewOffset);
		}

	}
```

MainActivity.cs
```C#
SlidingLayer slidingLayer;
SlidingLayerManager slidingLayerManager;

protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.activity_main);
			slidingLayer = FindViewById<SlidingLayer> (Resource.Id.slidingLayer1);
		    SetupSlidingView ();

		}
		
		public void SetupSlidingView ()
		{
			slidingLayerManager = new SlidingLayerManager (this, slidingLayer);
			slidingLayerManager.setupSlidingLayerPosition ("bottom");
			slidingLayerManager.setupSlidingLayerTransform ("none");
			slidingLayerManager.setupShadow (false);
			slidingLayerManager.setupLayerOffset (false);
			slidingLayerManager.setupPreviewMode (false);
		}
```
