using System;
using Android.Graphics;
using Android.Widget;
using Android.Views.Animations;
using Android.Views;
using Android.OS;
using Android.Graphics.Drawables;
using Android.Content;
using Android.Util;
using Android.Content.Res;
using Java.Lang;
using Android.Support.V4.View;

namespace SlidingLayerLib
{


	public class SlidingLayer : FrameLayout, IInterpolator
	{


		private const string STATE_KEY = "state";

		/**
		 * Special value for the position of the layer. STICK_TO_RIGHT means that
		 * the view shall be attached to the right side of the screen, and come from
		 * there into the viewable area.
		 */
		public const int STICK_TO_RIGHT = -1;

		/**
		 * Special value for the position of the layer. STICK_TO_LEFT means that the
		 * view shall be attached to the left side of the screen, and come from
		 * there into the viewable area.
		 */
		public const int STICK_TO_LEFT = -2;

		/**
		 * Special value for the position of the layer. STICK_TO_TOP means that the view will stay attached to the top
		 * part of the screen, and come from there into the viewable area.
		 */
		public const int STICK_TO_TOP = -3;

		/**
		 * Special value for the position of the layer. STICK_TO_BOTTOM means that the view will stay attached to the
		 * bottom part of the screen, and come from there into the viewable area.
		 */
		public const int STICK_TO_BOTTOM = -4;

		private const int HORIZONTAL = 0;
		private const int VERTICAL = 1;

		private const int HIGH_VELOCITY = 9000;
		private const int MAX_SCROLLING_DURATION = 600; // in ms
		private const int MIN_DISTANCE_FOR_FLING = 10; // in dip


		/**
		 * Sentinel value for no current active pointer. Used by {@link #mActivePointerId}.
		 */
		private const int INVALID_VALUE = -1;
		protected int mActivePointerId = INVALID_VALUE;
		protected VelocityTracker mVelocityTracker;
		protected int mMaximumVelocity;

		private Random mRandom;
		protected Bundle mState;

		private Scroller mScroller;

		private int mShadowSize;
		private Drawable mShadowDrawable;
		private bool mForceLayout;

		/**
		 * The size of the panel that sticks out when closed
		 */
		private int mOffsetDistance;

		private bool mDrawingCacheEnabled;
		private int mScreenSide;

		/**
		 * If the user taps the layer then we will switch state it if enabled.
		 */
		private bool changeStateOnTap = true;

		/**
		 * The size of the panel in preview mode
		 */
		private int mPreviewOffsetDistance = INVALID_VALUE;

		private bool mEnabled = true;
		private bool mSlidingFromShadowEnabled = true;
		private bool mIsDragging;
		private bool mIsUnableToDrag;
		private int mTouchSlop;

		private float mLastX = INVALID_VALUE;
		private float mLastY = INVALID_VALUE;

		private float mInitialX = INVALID_VALUE;
		private float mInitialY = INVALID_VALUE;
		private float mInitialRawX = INVALID_VALUE;
		private float mInitialRawY = INVALID_VALUE;

		/**
		 * Flags to determine the state of the layer
		 */
		private const int STATE_CLOSED = 0;
		private const int STATE_PREVIEW = 1;
		private const int STATE_OPENED = 2;

		private int mCurrentState;

		private bool mScrolling;

		private OnInteractListener mOnInteractListener;

		/**
		 * Optional callback to notify client when scroll position has changed
		 */
		private OnScrollListener mOnScrollListener;

		private int mMinimumVelocity;
		private int mFlingDistance;

		private LayerTransformer mLayerTransformer;

		public SlidingLayer (Context context) : this (context, null)
		{

		}

		public SlidingLayer (Context context, IAttributeSet attrs) : this (context, attrs, 0)
		{
			//this (context, attrs, 0);
		}

		/**
		 * Constructor for the sliding layer.<br>
		 * By default this panel will
		 * <ol>
		 * <li>{@link #setStickTo(int)} with param {@link #STICK_TO_RIGHT}</li>
		 * <li>Use no shadow drawable. (i.e. with size of 0)</li>
		 * <li>Close when the panel is tapped</li>
		 * <li>Open when the offset is tapped, but will have an offset of 0</li>
		 * </ol>
		 *
		 * @param context  a reference to an existing context
		 * @param attrs    attribute set constructed from attributes set in android .xml file
		 * @param defStyle style res id
		 */
		public SlidingLayer (Context context, IAttributeSet attrs, int defStyle) : base (context, attrs, defStyle)
		{
			//super (context, attrs, defStyle);

			// Style
			TypedArray ta = context.ObtainStyledAttributes (attrs, Resource.Styleable.SlidingLayer);

			// Set the side of the screen
			setStickTo (ta.GetInt (Resource.Styleable.SlidingLayer_stickTo, STICK_TO_RIGHT));

			// Sets the shadow drawable
			int shadowRes = ta.GetResourceId (Resource.Styleable.SlidingLayer_shadowDrawable, INVALID_VALUE);
			if (shadowRes != INVALID_VALUE) {
				setShadowDrawable (shadowRes);
			}

			// Sets the shadow size
			mShadowSize = (int)ta.GetDimension (Resource.Styleable.SlidingLayer_shadowSize, 0);

			// Sets the ability to open or close the layer by tapping in any empty space
			changeStateOnTap = ta.GetBoolean (Resource.Styleable.SlidingLayer_changeStateOnTap, true);

			// How much of the view sticks out when closed
			mOffsetDistance = ta.GetDimensionPixelOffset (Resource.Styleable.SlidingLayer_offsetDistance, 0);

			// Sets the size of the preview summary, if any
			mPreviewOffsetDistance = ta.GetDimensionPixelOffset (Resource.Styleable.SlidingLayer_previewOffsetDistance,
					INVALID_VALUE);

			// If showing offset is greater than preview mode offset dimension, exception is thrown
			checkPreviewModeConsistency ();

			ta.Recycle ();

			init ();
		}

		private void init ()
		{

			if (Build.VERSION.SdkInt >= Build.VERSION_CODES.Honeycomb) {
				//SetLayerType (LAYER_TYPE_HARDWARE, null);
				SetLayerType (LayerType.Hardware, null);
			}

			SetWillNotDraw (false);
			DescendantFocusability = DescendantFocusability.AfterDescendants;
			Focusable = true;

			Context context = Context;
			mScroller = new Scroller (context, this);

			ViewConfiguration configuration = ViewConfiguration.Get (context);
			mTouchSlop = ViewConfigurationCompat.GetScaledPagingTouchSlop (configuration);
			mMinimumVelocity = configuration.ScaledMinimumFlingVelocity;

			mMaximumVelocity = configuration.ScaledMaximumFlingVelocity;

			float density = context.Resources.DisplayMetrics.Density;
			mFlingDistance = (int)(MIN_DISTANCE_FOR_FLING * density);

			mRandom = new Random ();
		}

		/**
		 * Method exposing the state of the panel
		 *
		 * @return returns the state of the panel (@link STATE_OPENED, STATE_CLOSED or STATE_PREVIEW). Please note
		 * that if the panel was opened with smooth animation this method is not guaranteed to return
		 * its final value until the panel has reached its final position.
		 */
		private int getCurrentState ()
		{
			return mCurrentState;
		}

		public bool isOpened ()
		{
			return mCurrentState == STATE_OPENED;
		}

		public bool isInPreviewMode ()
		{
			return mCurrentState == STATE_PREVIEW;
		}

		public bool isClosed ()
		{
			return mCurrentState == STATE_CLOSED;
		}

		public void openLayer (bool smoothAnimation)
		{
			setLayerState (STATE_OPENED, smoothAnimation);
		}

		public void openPreview (bool smoothAnimation)
		{
			if (mPreviewOffsetDistance == INVALID_VALUE) {
				throw new IllegalStateException ("A value offset for the preview has to be specified in order to open " +
						"the layer in preview mode. Use setPreviewOffsetDistance or set its associated XML property ");
			}
			setLayerState (STATE_PREVIEW, smoothAnimation);
		}

		public void closeLayer (bool smoothAnimation)
		{
			setLayerState (STATE_CLOSED, smoothAnimation);
		}

		private void setLayerState (int state, bool smoothAnimation)
		{
			setLayerState (state, smoothAnimation, false);
		}

		private void setLayerState (int state, bool smoothAnimation, bool force)
		{
			setLayerState (state, smoothAnimation, force, 0, 0);
		}

		private void setLayerState (int state, bool smoothAnimation, bool force,
									int velocityX, int velocityY)
		{

			if (!force && mCurrentState == state) {
				BuildDrawingCache (false);
				return;
			}

			if (mOnInteractListener != null) {
				notifyActionStartedForState (state);
			}

			int [] pos = getDestScrollPosForState (state);

			if (smoothAnimation) {
				int velocity = allowedDirection () == HORIZONTAL ? velocityX : velocityY;
				smoothScrollTo (pos [0], pos [1], velocity);
			} else {
				completeScroll ();
				scrollToAndNotify (pos [0], pos [1]);
			}

			mCurrentState = state;
		}

		/**
		 * Sets the listener to be invoked after a switch change
		 * {@link OnInteractListener}.
		 *
		 * @param listener Listener to set
		 */
		public void setOnInteractListener (OnInteractListener listener)
		{
			mOnInteractListener = listener;
		}

		/**
		 * Sets the listener to be invoked when the layer is being scrolled
		 * {@link OnScrollListener}.
		 *
		 * @param listener Listener to set
		 */
		public void setOnScrollListener (OnScrollListener listener)
		{
			mOnScrollListener = listener;
		}

		/**
		 * Sets the transformer to use when the layer is being scrolled
		 * {@link LayerTransformer}.
		 *
		 * @param layerTransformer Transformer to adopt
		 */
		public void setLayerTransformer (LayerTransformer layerTransformer)
		{
			mLayerTransformer = layerTransformer;
		}

		/**
		 * Sets the shadow of the size which will be included within the view by
		 * using padding since it's on the left of the view in this case
		 *
		 * @param shadowSize Desired size of the shadow
		 * @see #getShadowSize()
		 * @see #setShadowDrawable(Drawable)
		 * @see #setShadowDrawable(int)
		 */
		public void setShadowSize (int shadowSize)
		{
			mShadowSize = shadowSize;
			Invalidate (Left, Top, Right, Bottom);
		}

		/**
		 * Sets the shadow size by the value of a resource.
		 *
		 * @param resId The dimension resource id to be set as the shadow size.
		 */
		public void setShadowSizeRes (int resId)
		{
			setShadowSize ((int)Context.Resources.GetDimension (resId));

		}

		/**
		 * Return the current size of the shadow.
		 *
		 * @return The size of the shadow in pixels
		 */
		public int getShadowSize ()
		{
			return mShadowSize;
		}

		/**
		 * Sets a drawable that will be used to create the shadow for the layer.
		 *
		 * @param d Drawable append as a shadow
		 */
		public void setShadowDrawable (Drawable d)
		{
			mShadowDrawable = d;
			RefreshDrawableState ();
			SetWillNotDraw (false);
			Invalidate (Left, Top, Right, Bottom);
		}

		/**
		 * Sets a drawable resource that will be used to create the shadow for the
		 * layer.
		 *
		 * @param resId Resource ID of a drawable
		 */
		public void setShadowDrawable (int resId)
		{
			setShadowDrawable (Context.Resources.GetDrawable (resId));
		}

		/**
		 * Sets the offset distance of the panel by using a dimension resource.
		 *
		 * @param resId The dimension resource id to be set as the offset.
		 */
		public void setOffsetDistanceRes (int resId)
		{
			setOffsetDistance ((int)Context.Resources.GetDimension (resId));
		}

		/**
		 * Sets the offset distance of the panel. How much sticks out when off screen.
		 *
		 * @param offsetDistance Size of the offset in pixels
		 * @see #getOffsetDistance()
		 */
		public void setOffsetDistance (int offsetDistance)
		{
			mOffsetDistance = offsetDistance;
			checkPreviewModeConsistency ();
			Invalidate (Left, Top, Right, Bottom);
		}

		/**
		 * @return returns the number of pixels that are visible when the panel is closed
		 */
		public int getOffsetDistance ()
		{
			return mOffsetDistance;
		}

		/**
		 * Sets the offset distance of the preview panel by using a dimension resource.
		 *
		 * @param resId The dimension resource id to be set as the size of the preview mode.
		 */
		public void setPreviewOffsetDistanceRes (int resId)
		{
			setPreviewOffsetDistance ((int)Context.Resources.GetDimension (resId));
		}

		/**
		 * Sets the size of the panel when in preview mode.
		 *
		 * @param previewOffsetDistance Size of the offset in pixels
		 * @see #getOffsetDistance()
		 */
		public void setPreviewOffsetDistance (int previewOffsetDistance)
		{
			mPreviewOffsetDistance = previewOffsetDistance;

			checkPreviewModeConsistency ();
			Invalidate (Left, Top, Right, Bottom);

			if (mCurrentState == STATE_PREVIEW) {
				smoothScrollToCurrentPosition ();
			}
		}

		private void checkPreviewModeConsistency ()
		{
			if (isPreviewModeEnabled () && mOffsetDistance > mPreviewOffsetDistance) {
				throw new IllegalStateException ("The showing offset of the layer can never be greater than the " +
						"offset dimension of the preview mode");
			}
		}

		/**
		 * @return true if the preview mode is enabled
		 */
		private bool isPreviewModeEnabled ()
		{
			return mPreviewOffsetDistance != INVALID_VALUE;
		}

		protected override bool VerifyDrawable (Drawable who)
		{
			return base.VerifyDrawable (who) || who == mShadowDrawable;
		}

		protected override void DrawableStateChanged ()
		{
			base.DrawableStateChanged ();
			Drawable d = mShadowDrawable;
			if (d != null && d.IsStateful) {
				d.SetState (GetDrawableState ());
			}
		}

		public bool isSlidingEnabled ()
		{
			return mEnabled;
		}

		public void setSlidingEnabled (bool _enabled)
		{
			mEnabled = _enabled;
		}

		public bool isSlidingFromShadowEnabled ()
		{
			return mSlidingFromShadowEnabled;
		}

		public void setSlidingFromShadowEnabled (bool _slidingShadow)
		{
			mSlidingFromShadowEnabled = _slidingShadow;
		}

		protected override IParcelable OnSaveInstanceState ()
		{
			IParcelable superState = base.OnSaveInstanceState ();
			SavedState state = new SavedState (superState);
			if (mState == null) {
				mState = new Bundle ();
			}

			mState.PutInt (STATE_KEY, mCurrentState);
			state.mState = mState;
			state.mState = mState;
			return state;
		}

		protected override void OnRestoreInstanceState (IParcelable state)
		{
			SavedState savedState = (SavedState)state;
			base.OnRestoreInstanceState (savedState.SuperState);
			restoreState (savedState.mState);

		}



		public void restoreState (IParcelable inn)
		{
			mState = (Bundle)inn;
			int state = mState.GetInt (STATE_KEY);
			setLayerState (state, true);
		}

		private float getViewX (MotionEvent evt)
		{
			return evt.GetX ();
		}

		private float getViewY (MotionEvent evt)
		{
			return evt.GetY ();
		}

		public override bool OnInterceptTouchEvent (MotionEvent ev)
		{
			if (!mEnabled) {
				return false;
			}

			var action = ev.Action & MotionEventActions.Mask;

			if (action == MotionEventActions.Cancel || action == MotionEventActions.Up) {
				mIsDragging = false;
				mIsUnableToDrag = false;
				mActivePointerId = INVALID_VALUE;
				if (mVelocityTracker != null) {
					mVelocityTracker.Recycle ();
					mVelocityTracker = null;
				}
				return false;
			}

			if (action != (int)MotionEventActions.Down) {
				if (mIsDragging) {
					return true;
				} else if (mIsUnableToDrag) {
					return false;
				}
			}

			switch (action) {
			case MotionEventActions.Move:

				int activePointerId = mActivePointerId;
				if (activePointerId == INVALID_VALUE) {
					break;
				}
				int pointerIndex = MotionEventCompat.FindPointerIndex (ev, activePointerId);
				float x = getViewX (ev);
				float dx = x - mLastX;
				float xDiff = Java.Lang.Math.Abs (dx);
				float y = getViewY (ev);
				float dy = y - mLastY;
				float yDiff = Java.Lang.Math.Abs (dy);

				if ((dx != 0 || dy != 0) &&
						canScroll (this, false, (int)dx, (int)dy, (int)x, (int)y)) {
					mLastX = mInitialRawX = x;
					mLastY = mInitialRawY = y;
					mInitialX = ev.GetX (pointerIndex);
					mInitialY = ev.GetY (pointerIndex);
					return false;

				}

				bool validHorizontalDrag = xDiff > mTouchSlop && xDiff > yDiff;
				bool validVerticalDrag = yDiff > mTouchSlop && yDiff > xDiff;

				if (validHorizontalDrag) {
					mLastX = x;
				} else if (validVerticalDrag) {
					mLastY = y;
				}

				if (validHorizontalDrag || validVerticalDrag) {
					mIsDragging = true;
					//setDrawingCacheEnabled (true);
					SetChildrenDrawingCacheEnabled (true);
				}
				break;

			case MotionEventActions.Down:
				mLastX = mInitialRawX = getViewX (ev);
				mLastY = mInitialRawY = getViewY (ev);
				mInitialX = ev.GetX (0);
				mInitialY = ev.GetY (0);
				mActivePointerId = MotionEventCompat.GetPointerId (ev, 0);

				if (touchPointIsWithinBounds (ev.GetX (), ev.GetY ())) {
					mIsDragging = false;
					mIsUnableToDrag = false;
					// We don't want to do anything, send the event up
					return base.OnInterceptTouchEvent (ev);
				} else {
					completeScroll ();
					mIsDragging = false;
					mIsUnableToDrag = true;
				}
				break;
			case MotionEventActions.PointerUp:
				onSecondaryPointerUp (ev);
				break;
			}

			if (!mIsDragging) {
				if (mVelocityTracker == null) {
					mVelocityTracker = VelocityTracker.Obtain ();
				}
				mVelocityTracker.AddMovement (ev);
			}

			return mIsDragging;
		}

		public override bool OnTouchEvent (MotionEvent ev)
		{
			if (ev.Action == MotionEventActions.Down && ev.EdgeFlags != 0) {
				return false;
			}

			if (!mEnabled || !mIsDragging && !touchPointIsWithinBounds (mInitialX, mInitialY)) {
				return false;
			}

			if (mVelocityTracker == null) {
				mVelocityTracker = VelocityTracker.Obtain ();
			}
			mVelocityTracker.AddMovement (ev);

			var action = ev.Action;

			int pointerIndex;
			float x, y;

			switch (action & MotionEventActions.Mask) {
			case MotionEventActions.Down: {
					completeScroll ();

					// Remember where the motion event started
					mLastX = mInitialRawX = getViewX (ev);
					mLastY = mInitialRawY = getViewY (ev);
					mInitialX = ev.GetX ();
					mInitialY = ev.GetY ();
					mActivePointerId = ev.GetPointerId (0);
					break;
				}

			case MotionEventActions.Move:

				pointerIndex = MotionEventCompat.FindPointerIndex (ev, mActivePointerId);

				if (!touchPointIsWithinBounds (ev.GetX (), ev.GetY (), false)) return false;

				x = getViewX (ev);
				y = getViewY (ev);

				float deltaX = mLastX - x;
				float deltaY = mLastY - y;

				mLastX = x;
				mLastY = y;

				if (!mIsDragging) {

					float xDiff = Java.Lang.Math.Abs (x - mInitialRawX);
					float yDiff = Java.Lang.Math.Abs (y - mInitialRawY);

					bool validHorizontalDrag = xDiff > mTouchSlop && xDiff > yDiff;
					bool validVerticalDrag = yDiff > mTouchSlop && yDiff > xDiff;

					if (validHorizontalDrag || validVerticalDrag) {
						mIsDragging = true;

						SetChildrenDrawingCacheEnabled (true);
					}
				}

				if (mIsDragging) {

					float oldScrollX = ScrollX;
					float oldScrollY = ScrollY;
					float scrollX = (int)(oldScrollX + deltaX);
					float scrollY = oldScrollY + deltaY;

					// Log.d("Layer", String.format("Layer scrollX[%f],scrollY[%f]", scrollX, scrollY));
					float leftBound, rightBound;
					float bottomBound, topBound;
					switch (mScreenSide) {
					case STICK_TO_LEFT:
						topBound = bottomBound = rightBound = 0;
						leftBound = Width; // How far left we can scroll
						break;
					case STICK_TO_RIGHT:
						rightBound = -Width;
						topBound = bottomBound = leftBound = 0;
						break;
					case STICK_TO_TOP:
						topBound = Height;
						bottomBound = rightBound = leftBound = 0;
						break;
					case STICK_TO_BOTTOM:
						bottomBound = -Height;
						topBound = rightBound = leftBound = 0;
						break;
					default:
						topBound = bottomBound = rightBound = leftBound = 0;
						break;
					}

					if (scrollX > leftBound) {
						scrollX = leftBound;
					} else if (scrollX < rightBound) {
						scrollX = rightBound;
					}
					if (scrollY > topBound) {
						scrollY = topBound;
					} else if (scrollY < bottomBound) {
						scrollY = bottomBound;
					}

					// Keep the precision
					mLastX += scrollX - (int)scrollX;
					mLastY += scrollY - (int)scrollY;

					scrollToAndNotify ((int)scrollX, (int)scrollY);
				}
				break;


			case MotionEventActions.Up:

				if (mIsDragging) {
					VelocityTracker velocityTracker = mVelocityTracker;
					velocityTracker.ComputeCurrentVelocity (1000, mMaximumVelocity);
					int initialVelocityX = (int)VelocityTrackerCompat.GetXVelocity (velocityTracker,
							mActivePointerId);
					int initialVelocityY = (int)VelocityTrackerCompat.GetYVelocity (velocityTracker,
							mActivePointerId);
					int scrollX = ScrollX;
					int scrollY = ScrollY;

					pointerIndex = MotionEventCompat.FindPointerIndex (ev, mActivePointerId);
					x = getViewX (ev);
					y = getViewY (ev);

					int nextState = determineNextStateForDrag (scrollX, scrollY, initialVelocityX, initialVelocityY,
							(int)mInitialRawX, (int)mInitialRawY, (int)x, (int)y);
					setLayerState (nextState, true, true, initialVelocityX, initialVelocityY);

					mActivePointerId = INVALID_VALUE;
					endDrag ();

				} else if (changeStateOnTap) {
					int nextState = determineNextStateAfterTap ();
					setLayerState (nextState, true, true);
				}
				break;


			case MotionEventActions.Cancel:
				if (mIsDragging) {
					setLayerState (mCurrentState, true, true);
					mActivePointerId = INVALID_VALUE;
					endDrag ();
				}
				break;

			case MotionEventActions.PointerDown: {
					pointerIndex = MotionEventCompat.GetActionIndex (ev);
					mActivePointerId = ev.GetPointerId (pointerIndex);
					mLastX = getViewX (ev);
					mLastY = getViewY (ev);
					break;

				}
			case MotionEventActions.PointerUp: {
					onSecondaryPointerUp (ev);
					pointerIndex = MotionEventCompat.FindPointerIndex (ev, mActivePointerId);
					mLastX = getViewX (ev);
					mLastY = getViewY (ev);
					break;
				}
			}

			return true;
		}

		/**
		 * Checks if it's allowed to slide from the given position.
		 *
		 * @param touchX where the touch event started
		 * @param touchY where the touch event started.
		 * @return true if you can drag this view, false otherwise
		 */
		private bool touchPointIsWithinBounds (float touchX, float touchY)
		{
			return touchPointIsWithinBounds (touchX, touchY, true);
		}

		private bool touchPointIsWithinBounds (float touchX, float touchY, bool withinLayer)
		{

			int scroll = 0;
			float touch;

			if (allowedDirection () == HORIZONTAL) {
				if (withinLayer) scroll = ScrollX;
				touch = touchX;
			} else {
				if (withinLayer) scroll = ScrollY;
				touch = touchY;
			}

			switch (mScreenSide) {
			case STICK_TO_RIGHT:
			case STICK_TO_BOTTOM:
				return touch >= -scroll;
			case STICK_TO_LEFT:
				return touch <= Width - scroll;
			case STICK_TO_TOP:
				return touch <= Height - scroll;
			default:
				throw new IllegalStateException ("The layer has to be stuck to one of the sides of the screen. " +
						"Current value is: " + mScreenSide);
			}
		}

		protected bool canScroll (View v, bool checkV, int dx, int dy, int x, int y)
		{

			if (v is ViewGroup) {
				ViewGroup group = (ViewGroup)v;
				int scrollX = v.ScrollX;
				int scrollY = v.ScrollY;

				int count = group.ChildCount;
				// Count backwards - let topmost views consume scroll distance first.
				for (int i = count - 1; i >= 0; i--) {

					// This will not work for transformed views in Honeycomb+
					View child = group.GetChildAt (i);
					if (x + scrollX >= child.Left && x + scrollX < child.Right &&
						y + scrollY >= child.Top && y + scrollY < child.Bottom &&
						canScroll (child, true, dx, dy, x + scrollX - child.Left,
								   y + scrollY - child.Top)) {
						return true;
					}
				}
			}

			return checkV && (
				(allowedDirection () == HORIZONTAL && ViewCompat.CanScrollHorizontally (v, -dx) ||
				 allowedDirection () == VERTICAL && ViewCompat.CanScrollVertically (v, -dy)));
		}

		/**
		 * Based on the position and velocity of the layer we calculate what the next state should be.
		 *
		 * @param velocityX
		 * @param velocityY
		 * @param initialX
		 * @param initialY
		 * @param currentX
		 * @param currentY
		 * @return the state of the panel (@link STATE_OPENED, STATE_CLOSED or STATE_PREVIEW).
		 */
		private int determineNextStateForDrag (int scrollX, int scrollY, int velocityX,
											   int velocityY, int initialX, int initialY,
											  int currentX, int currentY)
		{

			int panelOffset;
			int panelSize;
			int relativeVelocity;
			int absoluteDelta;

			if (allowedDirection () == HORIZONTAL) {
				panelSize = Width;
				panelOffset = Java.Lang.Math.Abs (panelSize - Java.Lang.Math.Abs (scrollX));
				absoluteDelta = Java.Lang.Math.Abs (currentX - initialX);
				relativeVelocity = velocityX * (mScreenSide == STICK_TO_LEFT ? 1 : -1);
			} else {
				panelSize = Height;
				panelOffset = Java.Lang.Math.Abs (panelSize - Java.Lang.Math.Abs (scrollY));
				absoluteDelta = Java.Lang.Math.Abs (currentY - initialY);
				relativeVelocity = velocityY * (mScreenSide == STICK_TO_TOP ? 1 : -1);
			}

			int absoluteVelocity = Java.Lang.Math.Abs (relativeVelocity);
			bool isOverThreshold = absoluteDelta > mFlingDistance && absoluteVelocity > mMinimumVelocity;

			if (isOverThreshold) {

				if (relativeVelocity > 0) {
					return STATE_OPENED;
				} else {

					bool goesToPreview = isPreviewModeEnabled ()
							&& panelOffset > mPreviewOffsetDistance
							&& absoluteVelocity < HIGH_VELOCITY;

					if (goesToPreview) {
						return STATE_PREVIEW;
					} else {
						return STATE_CLOSED;
					}
				}

			} else {

				int openedThreshold = (panelSize + (isPreviewModeEnabled () ? mPreviewOffsetDistance : 0)) / 2;

				if (panelOffset > openedThreshold) {
					return STATE_OPENED;
				} else if (isPreviewModeEnabled () && panelOffset > mPreviewOffsetDistance / 2) {
					return STATE_PREVIEW;
				} else {
					return STATE_CLOSED;
				}
			}
		}

		/**
		 * Based on the current state of the panel, this method returns the next state after tapping.
		 *
		 * @return the state of the panel (@link STATE_OPENED, STATE_CLOSED or STATE_PREVIEW).
		 */
		private int determineNextStateAfterTap ()
		{

			switch (mCurrentState) {
			case STATE_CLOSED:
				return isPreviewModeEnabled () ? STATE_PREVIEW : STATE_OPENED;
			case STATE_PREVIEW:
				return STATE_OPENED;
			case STATE_OPENED:
				return isPreviewModeEnabled () ? STATE_PREVIEW : STATE_CLOSED;
			}

			return STATE_CLOSED;
		}

		/**
		 * Like {@link View#scrollBy}, but scroll smoothly instead of immediately.
		 *
		 * @param x the number of pixels to scroll by on the X axis
		 * @param y the number of pixels to scroll by on the Y axis
		 */
		void smoothScrollTo (int x, int y)
		{
			smoothScrollTo (x, y, 0);
		}

		/**
		 * Like {@link View#scrollBy}, but scroll smoothly instead of immediately.
		 *
		 * @param x        the number of pixels to scroll by on the X axis
		 * @param y        the number of pixels to scroll by on the Y axis
		 * @param velocity the velocity associated with a fling, if applicable. (0
		 *                 otherwise)
		 */
		void smoothScrollTo (int x, int y, int velocity)
		{

			if (ChildCount == 0) {
				SetChildrenDrawingCacheEnabled (false);
				return;
			}

			int sx = ScrollX;
			int sy = ScrollY;
			int dx = x - sx;
			int dy = y - sy;
			if (dx == 0 && dy == 0) {
				completeScroll ();
				if (mOnInteractListener != null) {
					notifyActionFinished ();
				}
				return;
			}

			SetChildrenDrawingCacheEnabled (true);
			mScrolling = true;

			int width = Width;
			int halfWidth = width / 2;
			float distanceRatio = Java.Lang.Math.Min (1f, 1.0f * Java.Lang.Math.Abs (dx) / width);
			float distance = halfWidth + halfWidth * distanceInfluenceForSnapDuration (distanceRatio);

			int duration;
			velocity = Java.Lang.Math.Abs (velocity);
			if (velocity > 0) {
				duration = 4 * Java.Lang.Math.Round (1000 * Java.Lang.Math.Abs (distance / velocity));
			} else {
				duration = MAX_SCROLLING_DURATION;
			}
			duration = Java.Lang.Math.Min (duration, MAX_SCROLLING_DURATION);

			mScroller.StartScroll (sx, sy, dx, dy, duration);
			ViewCompat.PostInvalidateOnAnimation (this);
		}

		private void smoothScrollToCurrentPosition ()
		{
			int [] pos = getDestScrollPosForState (mCurrentState);
			smoothScrollTo (pos [0], pos [1]);
		}

		// We want the duration of the page snap animation to be influenced by the
		// distance that
		// the screen has to travel, however, we don't want this duration to be
		// effected in a
		// purely linear fashion. Instead, we use this method to moderate the effect
		// that the distance
		// of travel has on the overall snap duration.
		float distanceInfluenceForSnapDuration (float f)
		{
			f -= 0.5f; // center the values about 0.
			f *= 0.3f * (float)Java.Lang.Math.Pi / 2.0f;

			return (float)Java.Lang.Math.Sin (f);
		}

		private void endDrag ()
		{
			mIsDragging = false;
			mIsUnableToDrag = false;

			if (mVelocityTracker != null) {
				mVelocityTracker.Recycle ();
				mVelocityTracker = null;
			}
		}


		public override void BuildDrawingCache (bool enabled)
		{
			if (mDrawingCacheEnabled != enabled) {
				base.BuildDrawingCache (enabled);
				mDrawingCacheEnabled = enabled;

				int l = ChildCount;
				for (int i = 0; i < l; i++) {
					View child = GetChildAt (i);
					if (child.Visibility != ViewStates.Gone) {
						child.BuildDrawingCache (enabled);
					}
				}
			}
		}

		protected override void SetChildrenDrawingCacheEnabled (bool enabled)
		{
			if (mDrawingCacheEnabled != enabled) {
				base.SetChildrenDrawingCacheEnabled (enabled);
				mDrawingCacheEnabled = enabled;

				int l = ChildCount;
				for (int i = 0; i < l; i++) {
					View child = GetChildAt (i);
					if (child.Visibility != ViewStates.Gone) {
						child.DrawingCacheEnabled = enabled;
					}
				}
			}
		}


		private void onSecondaryPointerUp (MotionEvent ev)
		{
			int pointerIndex = ev.ActionIndex;
			int pointerId = ev.GetPointerId (pointerIndex);
			if (pointerId == mActivePointerId) {
				// This was our active pointer going up. Choose a new
				// active pointer and adjust accordingly.
				int newPointerIndex = pointerIndex == 0 ? 1 : 0;
				mLastX = ev.GetX (newPointerIndex);
				mActivePointerId = ev.GetPointerId (newPointerIndex);
				if (mVelocityTracker != null) {
					mVelocityTracker.Clear ();
				}
			}
		}

		private void completeScroll ()
		{

			bool needPopulate = mScrolling;
			if (needPopulate) {
				// Done with scroll, no longer want to cache view drawing.
				SetChildrenDrawingCacheEnabled (false);
				mScroller.AbortAnimation ();
				int oldX = ScrollX;
				int oldY = ScrollY;
				int x = mScroller.CurrX;
				int y = mScroller.CurrY;
				if (oldX != x || oldY != y) {
					scrollToAndNotify (x, y);
				}
				if (mOnInteractListener != null) {
					notifyActionFinished ();
				}
			}
			mScrolling = false;
		}

		private void scrollToAndNotify (int x, int y)
		{

			ScrollTo (x, y);

			if (mOnScrollListener == null && mLayerTransformer == null) {
				return;
			}

			int scroll;
			if (allowedDirection () == VERTICAL) {
				scroll = Height - Java.Lang.Math.Abs (y);
			} else {
				scroll = Width - Java.Lang.Math.Abs (x);
			}

			if (mOnScrollListener != null) {
				mOnScrollListener.onScroll (Java.Lang.Math.Abs (scroll));
			}

			if (mLayerTransformer != null) {

				int absoluteScroll = Java.Lang.Math.Abs (scroll);
				int layerSize = allowedDirection () == HORIZONTAL ? MeasuredWidth : MeasuredHeight;

				float layerProgress = (float)absoluteScroll / layerSize;
				float previewProgress = mPreviewOffsetDistance > 0 ?
					Java.Lang.Math.Min (1, (float)absoluteScroll / mPreviewOffsetDistance) :
						0;

				mLayerTransformer.InternalTransform (this, previewProgress, layerProgress, mScreenSide);
			}
		}

		/**
		 * Sets the default location where the SlidingLayer will appear
		 *
		 * @param screenSide The location where the Sliding layer will appear. Possible values are
		 *                   {@link #STICK_TO_BOTTOM}, {@link #STICK_TO_LEFT}
		 *                   {@link #STICK_TO_RIGHT}, {@link #STICK_TO_TOP}
		 */
		public void setStickTo (int screenSide)
		{
			mForceLayout = true;
			mScreenSide = screenSide;
			setLayerState (STATE_CLOSED, false, true);
		}

		private int allowedDirection ()
		{

			if (mScreenSide == STICK_TO_TOP || mScreenSide == STICK_TO_BOTTOM) {
				return VERTICAL;
			} else if (mScreenSide == STICK_TO_LEFT || mScreenSide == STICK_TO_RIGHT) {
				return HORIZONTAL;
			}

			return VERTICAL;
			//throw new IllegalStateException ("The screen side of the layer is illegal");
		}

		/**
		 * Sets the behavior when tapping the sliding layer
		 *
		 * @param changeStateOnTap
		 */
		public void setChangeStateOnTap (bool changeStateOnTap)
		{
			this.changeStateOnTap = changeStateOnTap;
		}

		protected override void OnMeasure (int widthMeasureSpec, int heightMeasureSpec)
		{
			int width = GetDefaultSize (0, widthMeasureSpec);
			int height = GetDefaultSize (0, heightMeasureSpec);
			SetMeasuredDimension (width, height);

			if (mLayerTransformer != null) {
				mLayerTransformer.OnMeasure (this, mScreenSide);


			}

			base.OnMeasure (GetChildMeasureSpec (widthMeasureSpec, 0, width),
							GetChildMeasureSpec (heightMeasureSpec, 0, height));
		}


		protected override void OnSizeChanged (int w, int h, int oldw, int oldh)
		{
			base.OnSizeChanged (w, h, oldw, oldh);

			bool scrollMustChange = false;
			if (allowedDirection () == VERTICAL) {
				if (h != oldh) {
					scrollMustChange = true;
				}
			} else if (w != oldw) {
				scrollMustChange = true;
			}

			if (scrollMustChange) {
				completeScroll ();
				int [] pos = getDestScrollPosForState (mCurrentState);
				ScrollTo (pos [0], pos [1]);
			}
		}

		protected override void OnLayout (bool changed, int left, int top, int right, int bottom)
		{

			if (mForceLayout) {
				mForceLayout = false;
				adjustLayoutParams ();

				if (mScreenSide == STICK_TO_RIGHT) {
					SetPadding (PaddingLeft + mShadowSize, PaddingTop, PaddingRight, PaddingBottom);
				} else if (mScreenSide == STICK_TO_BOTTOM) {
					SetPadding (PaddingLeft, PaddingTop + mShadowSize, PaddingRight, PaddingBottom);
				} else if (mScreenSide == STICK_TO_LEFT) {
					SetPadding (PaddingLeft, PaddingTop, PaddingRight + mShadowSize, PaddingBottom);
				} else if (mScreenSide == STICK_TO_TOP) {
					SetPadding (PaddingLeft, PaddingTop, PaddingRight, PaddingBottom + mShadowSize);
				}
			}

			base.OnLayout (changed, left, top, right, bottom);
		}

		protected override void OnDraw (Canvas canvas)
		{
			base.OnDraw (canvas);
		}


		private void adjustLayoutParams ()
		{

			ViewGroup.LayoutParams baseParams = LayoutParameters;

			if (baseParams is LayoutParams) {

				LayoutParams layoutParams = (LayoutParams)baseParams;

				switch (mScreenSide) {
				case STICK_TO_BOTTOM:
					layoutParams.Gravity = GravityFlags.Bottom;
					break;
				case STICK_TO_LEFT:
					layoutParams.Gravity = GravityFlags.Left;
					break;
				case STICK_TO_RIGHT:
					layoutParams.Gravity = GravityFlags.Right;
					break;
				case STICK_TO_TOP:
					layoutParams.Gravity = GravityFlags.Top;
					break;
				}

				LayoutParameters = baseParams;


			} else if (baseParams is RelativeLayout.LayoutParams) {

				RelativeLayout.LayoutParams layoutParams = (RelativeLayout.LayoutParams)baseParams;

				switch (mScreenSide) {
				case STICK_TO_BOTTOM:
					layoutParams.AddRule (LayoutRules.AlignParentBottom);
					break;
				case STICK_TO_LEFT:
					layoutParams.AddRule (LayoutRules.AlignParentLeft);
					break;
				case STICK_TO_RIGHT:
					layoutParams.AddRule (LayoutRules.AlignParentRight);
					break;
				case STICK_TO_TOP:
					layoutParams.AddRule (LayoutRules.AlignParentTop);
					break;
				}
			}

		}

		/**
		 * Get the destination position based on the velocity
		 *
		 * @return
		 * @since 1.0
		 */
		private int [] getDestScrollPosForState (int state)
		{

			int [] pos = new int [2];

			if (state == STATE_OPENED) {
				return pos;
			} else {

				int layerOffset = state == STATE_CLOSED ? mOffsetDistance : mPreviewOffsetDistance;

				switch (mScreenSide) {
				case STICK_TO_RIGHT:
					pos [0] = -Width + layerOffset;
					break;
				case STICK_TO_LEFT:
					pos [0] = Width - layerOffset;
					break;
				case STICK_TO_TOP:
					pos [1] = Height - layerOffset;
					break;
				case STICK_TO_BOTTOM:
					pos [1] = -Height + layerOffset;
					break;
				}

				return pos;
			}
		}

		public int getContentLeft ()
		{
			return Left + PaddingLeft;
		}

		protected override void DispatchDraw (Canvas canvas)
		{
			base.DispatchDraw (canvas);
			// Draw the margin drawable if needed.
			if (mShadowSize > 0 && mShadowDrawable != null) {
				if (mScreenSide == STICK_TO_RIGHT) {
					mShadowDrawable.SetBounds (0, 0, mShadowSize, Height);
				}
				if (mScreenSide == STICK_TO_TOP) {
					mShadowDrawable.SetBounds (0, Height - mShadowSize, Width, Height);
				}
				if (mScreenSide == STICK_TO_LEFT) {
					mShadowDrawable.SetBounds (Width - mShadowSize, 0, Width, Height);
				}
				if (mScreenSide == STICK_TO_BOTTOM) {
					mShadowDrawable.SetBounds (0, 0, Width, mShadowSize);
				}
				mShadowDrawable.Draw (canvas);
			}
		}

		public override void ComputeScroll ()
		{
			if (!mScroller.IsFinished) {
				if (mScroller.ComputeScrollOffset ()) {
					int oldX = ScrollX;
					int oldY = ScrollY;
					int x = mScroller.CurrX;
					int y = mScroller.CurrY;

					if (oldX != x || oldY != y) {
						scrollToAndNotify (x, y);
					}

					// Keep on drawing until the animation has finished.
					ViewCompat.PostInvalidateOnAnimation (this);
					return;
				}
			}

			// Done with scroll, clean up state.
			completeScroll ();
		}



		/**
		 * Handler interface for obtaining updates on the <code>SlidingLayer</code>'s state.
		 * <code>OnInteractListener</code> allows for external classes to be notified when the <code>SlidingLayer</code>
		 * receives input to be opened or closed.
		 */
		public interface OnInteractListener
		{

			/**
			 * This method is called when an attempt is made to open the current <code>SlidingLayer</code>. Note
			 * that because of animation, the <code>SlidingLayer</code> may not be visible yet.
			 */
			void onOpen ();

			/**
			 * This method is called when an attempt is made to show the preview mode in the current
			 * <code>SlidingLayer</code>. Note that because of animation, the <code>SlidingLayer</code> may not be
			 * visible yet.
			 */
			void onShowPreview ();

			/**
			 * This method is called when an attempt is made to close the current <code>SlidingLayer</code>. Note
			 * that because of animation, the <code>SlidingLayer</code> may still be visible.
			 */
			void onClose ();

			/**
			 * this method is executed after <code>onOpen()</code>, when the animation has finished.
			 */
			void onOpened ();

			/**
			 * this method is executed after <code>onShowPreview()</code>, when the animation has finished.
			 */
			void onPreviewShowed ();

			/**
			 * this method is executed after <code>onClose()</code>, when the animation has finished and the
			 * <code>SlidingLayer</code> is
			 * therefore no longer visible.
			 */
			void onClosed ();
		}

		private void notifyActionStartedForState (int state)
		{

			switch (state) {
			case STATE_CLOSED:
				mOnInteractListener.onClose ();
				break;

			case STATE_PREVIEW:
				mOnInteractListener.onShowPreview ();
				break;

			case STATE_OPENED:
				mOnInteractListener.onOpen ();
				break;
			}
		}

		private void notifyActionFinished ()
		{

			switch (mCurrentState) {
			case STATE_CLOSED:
				mOnInteractListener.onClosed ();
				break;

			case STATE_PREVIEW:
				mOnInteractListener.onPreviewShowed ();
				break;

			case STATE_OPENED:
				mOnInteractListener.onOpened ();
				break;
			}
		}

		public float GetInterpolation (float t)
		{
			t -= 1.0f;
			return (float)Java.Lang.Math.Pow (t, 5) + 1.0f;
		}

		/**
		 * Interface definition for a callback to be invoked when the layer has been scrolled.
		 */
		public interface OnScrollListener
		{

			/**
			 * Callback method to be invoked when the layer has been scrolled. This will be
			 * called after the scroll has completed
			 *
			 * @param absoluteScroll The absolute scrolling delta relative to the position of the container
			 */
			void onScroll (int absoluteScroll);
		}

		public class SavedState : BaseSavedState, IParcelableCreator
		{

			public Bundle mState;


			public SavedState (IParcelable superState) : base (superState)
			{
			}

			public SavedState (Parcel inn) : base (inn)
			{
				mState = inn.ReadBundle ();
			}

			public Java.Lang.Object CreateFromParcel (Parcel source)
			{
				return new SavedState (source);
			}

			public Java.Lang.Object [] NewArray (int size)
			{
				return new SavedState [size];
			}

			public override void WriteToParcel (Parcel dest, ParcelableWriteFlags flags)
			{
				base.WriteToParcel (dest, flags);
				dest.WriteBundle (mState);
			}

			//public static Creato<SavedState> CREATOR = new IParcelableCreator<SavedState> () {
			//	//@Override

			//	//public SavedState createFromParcel (Parcel in)
			//	//{
			//	//	return new SavedState (in);
			//	//}

			//	//@Override
			//	//public SavedState [] newArray (int size)
			//	//{
			//	//	return new SavedState [size];
			//	//}


			//};
		}
	}
}

