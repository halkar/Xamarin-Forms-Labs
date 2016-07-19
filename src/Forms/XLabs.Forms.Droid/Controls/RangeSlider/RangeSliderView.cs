﻿using System;
using System.Globalization;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using XLabs.Forms.Droid;

namespace XLabs.Forms.Controls
{
    public class RangeSliderView : ImageView
    {
        public static readonly Color ActiveColor = Color.Argb(0xFF, 0x33, 0xB5, 0xE5);
        /**
         * An invalid pointer id.
         */
        public static readonly int InvalidPointerId = 255;

        // Localized constants from MotionEvent for compatibility
        // with API < 8 "Froyo".
        public static readonly int ActionPointerIndexMask = 0x0000ff00, ActionPointerIndexShift = 8;

        public static readonly int DefaultMinimum = 0;
        public static readonly int DefaultMaximum = 100;
        public static readonly int HeightInDp = 30;
        public static readonly int TextLateralPaddingInDp = 3;

        private static readonly int InitialPaddingInDp = 8;
        private static readonly int DefaultTextSizeInDp = 14;
        private static readonly int DefaultTextDistanceToButtonInDp = 8;
        private static readonly int DefaultTextDistanceToTopInDp = 8;

        private static readonly int LineHeightInDp = 1;
        private readonly Paint _paint = new Paint(PaintFlags.AntiAlias);
        private readonly Paint _shadowPaint = new Paint();
        private readonly Matrix _thumbShadowMatrix = new Matrix();
        private readonly Path _translatedThumbShadowPath = new Path();

        private bool _activateOnDefaultValues;
        private Color _activeColor;

        private int _activePointerId = InvalidPointerId;
        private bool _alwaysActive;
        private Color _defaultColor;
        private int _distanceToTop;

        private float _downMotionX;
        private float _internalPad;

        private bool _isDragging;

        private float _padding;
        private Thumb? _pressedThumb;
        private RectF _rect;

        private int _scaledTouchSlop;
        private bool _showLabels;
        private bool _showTextAboveThumbs;

        private bool _singleThumb;
        private Color _textAboveThumbsColor;

        private int _textOffset;
        private int _textSize;
        private Bitmap _thumbDisabledImage;
        private float _thumbHalfHeight;

        private float _thumbHalfWidth;

        private Bitmap _thumbImage;
        private Bitmap _thumbPressedImage;

        private bool _thumbShadow;
        private int _thumbShadowBlur;
        private Path _thumbShadowPath;
        private int _thumbShadowXOffset;
        private int _thumbShadowYOffset;
        protected float AbsoluteMinValue, AbsoluteMaxValue;
        protected float AbsoluteMinValuePrim, AbsoluteMaxValuePrim;
        protected float MinDeltaForDefault = 0;
        protected float NormalizedMaxValue = 1f;
        protected float NormalizedMinValue;

        protected RangeSliderView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public RangeSliderView(Context context) : base(context)
        {
            Init(context, null);
        }

        public RangeSliderView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Init(context, attrs);
        }

        public RangeSliderView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            Init(context, attrs);
        }

        public RangeSliderView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes)
            : base(context, attrs, defStyleAttr, defStyleRes)
        {
            Init(context, attrs);
        }

        /// <summary>
        ///     Should the widget notify the listener callback while the user is still dragging a thumb? Default is false.
        /// </summary>
        public bool NotifyWhileDragging { get; set; }


        private float extractNumericValueFromAttributes(TypedArray a, int attribute, int defaultValue)
        {
            var tv = a.PeekValue(attribute);
            return tv == null ? defaultValue : a.GetFloat(attribute, defaultValue);
        }

        private void Init(Context context, IAttributeSet attrs)
        {
            float barHeight;
            var thumbNormal = Resource.Drawable.seek_thumb_normal;
            var thumbPressed = Resource.Drawable.seek_thumb_pressed;
            var thumbDisabled = Resource.Drawable.seek_thumb_disabled;
            Color thumbShadowColor;
            var defaultShadowColor = Color.Argb(75, 0, 0, 0);
            var defaultShadowYOffset = PixelUtil.DpToPx(context, 2);
            var defaultShadowXOffset = PixelUtil.DpToPx(context, 0);
            var defaultShadowBlur = PixelUtil.DpToPx(context, 2);

            if (attrs == null)
            {
                SetRangeToDefaultValues();
                _internalPad = PixelUtil.DpToPx(context, InitialPaddingInDp);
                barHeight = PixelUtil.DpToPx(context, LineHeightInDp);
                _activeColor = ActiveColor;
                _defaultColor = Color.Gray;
                _alwaysActive = false;
                _showTextAboveThumbs = true;
                _textAboveThumbsColor = Color.White;
                thumbShadowColor = defaultShadowColor;
                _thumbShadowXOffset = defaultShadowXOffset;
                _thumbShadowYOffset = defaultShadowYOffset;
                _thumbShadowBlur = defaultShadowBlur;
                _activateOnDefaultValues = false;
            }
            else
            {
                var a = Context.ObtainStyledAttributes(attrs, Resource.Styleable.RangeSliderView, 0, 0);
                try
                {
                    SetRangeValues(
                        extractNumericValueFromAttributes(a, Resource.Styleable.RangeSliderView_absoluteMinValue,
                            DefaultMinimum),
                        extractNumericValueFromAttributes(a, Resource.Styleable.RangeSliderView_absoluteMaxValue,
                            DefaultMaximum));
                    _showTextAboveThumbs = a.GetBoolean(Resource.Styleable.RangeSliderView_valuesAboveThumbs, true);
                    _textAboveThumbsColor = a.GetColor(Resource.Styleable.RangeSliderView_textAboveThumbsColor, Color.White);
                    _singleThumb = a.GetBoolean(Resource.Styleable.RangeSliderView_singleThumb, false);
                    _showLabels = a.GetBoolean(Resource.Styleable.RangeSliderView_showLabels, true);
                    _internalPad = a.GetDimensionPixelSize(Resource.Styleable.RangeSliderView_internalPadding,
                        InitialPaddingInDp);
                    barHeight = a.GetDimensionPixelSize(Resource.Styleable.RangeSliderView_barHeight, LineHeightInDp);
                    _activeColor = a.GetColor(Resource.Styleable.RangeSliderView_activeColor, ActiveColor);
                    _defaultColor = a.GetColor(Resource.Styleable.RangeSliderView_defaultColor, Color.Gray);
                    _alwaysActive = a.GetBoolean(Resource.Styleable.RangeSliderView_alwaysActive, false);

                    var normalDrawable = a.GetDrawable(Resource.Styleable.RangeSliderView_thumbNormal);
                    if (normalDrawable != null)
                    {
                        _thumbImage = BitmapUtil.DrawableToBitmap(normalDrawable);
                    }
                    var disabledDrawable = a.GetDrawable(Resource.Styleable.RangeSliderView_thumbDisabled);
                    if (disabledDrawable != null)
                    {
                        _thumbDisabledImage = BitmapUtil.DrawableToBitmap(disabledDrawable);
                    }
                    var pressedDrawable = a.GetDrawable(Resource.Styleable.RangeSliderView_thumbPressed);
                    if (pressedDrawable != null)
                    {
                        _thumbPressedImage = BitmapUtil.DrawableToBitmap(pressedDrawable);
                    }
                    _thumbShadow = a.GetBoolean(Resource.Styleable.RangeSliderView_thumbShadow, false);
                    thumbShadowColor = a.GetColor(Resource.Styleable.RangeSliderView_thumbShadowColor, defaultShadowColor);
                    _thumbShadowXOffset = a.GetDimensionPixelSize(Resource.Styleable.RangeSliderView_thumbShadowXOffset,
                        defaultShadowXOffset);
                    _thumbShadowYOffset = a.GetDimensionPixelSize(Resource.Styleable.RangeSliderView_thumbShadowYOffset,
                        defaultShadowYOffset);
                    _thumbShadowBlur = a.GetDimensionPixelSize(Resource.Styleable.RangeSliderView_thumbShadowBlur,
                        defaultShadowBlur);

                    _activateOnDefaultValues = a.GetBoolean(Resource.Styleable.RangeSliderView_activateOnDefaultValues,
                        false);
                }
                finally
                {
                    a.Recycle();
                }
            }

            if (_thumbImage == null)
            {
                _thumbImage = BitmapFactory.DecodeResource(Resources, thumbNormal);
            }
            if (_thumbPressedImage == null)
            {
                _thumbPressedImage = BitmapFactory.DecodeResource(Resources, thumbPressed);
            }
            if (_thumbDisabledImage == null)
            {
                _thumbDisabledImage = BitmapFactory.DecodeResource(Resources, thumbDisabled);
            }

            _thumbHalfWidth = 0.5f*_thumbImage.Width;
            _thumbHalfHeight = 0.5f*_thumbImage.Height;

            SetValuePrimAndNumberType();

            _textSize = PixelUtil.DpToPx(context, DefaultTextSizeInDp);
            _distanceToTop = PixelUtil.DpToPx(context, DefaultTextDistanceToTopInDp);
            _textOffset = !_showTextAboveThumbs
                ? 0
                : _textSize + PixelUtil.DpToPx(context,
                    DefaultTextDistanceToButtonInDp) + _distanceToTop;

            _rect = new RectF(_padding,
                _textOffset + _thumbHalfHeight - barHeight/2,
                Width - _padding,
                _textOffset + _thumbHalfHeight + barHeight/2);

            // make RangeSliderView focusable. This solves focus handling issues in case EditText widgets are being used along with the RangeSliderView within ScrollViews.
            Focusable = true;
            FocusableInTouchMode = true;
            _scaledTouchSlop = ViewConfiguration.Get(Context).ScaledTouchSlop;

            if (_thumbShadow)
            {
                // We need to remove hardware acceleration in order to blur the shadow
                SetLayerType(LayerType.Software, null);
                _shadowPaint.Color = thumbShadowColor;
                _shadowPaint.SetMaskFilter(new BlurMaskFilter(_thumbShadowBlur, BlurMaskFilter.Blur.Normal));
                _thumbShadowPath = new Path();
                _thumbShadowPath.AddCircle(0, 0,
                    _thumbHalfHeight,
                    Path.Direction.Cw);
            }
        }

        public void SetRangeValues(float minValue, float maxValue)
        {
            AbsoluteMinValue = minValue;
            AbsoluteMaxValue = maxValue;
            SetValuePrimAndNumberType();
        }

        public void SetTextAboveThumbsColor(Color textAboveThumbsColor)
        {
            _textAboveThumbsColor = textAboveThumbsColor;
            Invalidate();
        }

        public void SetTextAboveThumbsColorResource(int resId)
        {
            SetTextAboveThumbsColor(Resources.GetColor(resId));
        }


        // only used to set default values when initialised from XML without any values specified
        private void SetRangeToDefaultValues()
        {
            AbsoluteMinValue = DefaultMinimum;
            AbsoluteMaxValue = DefaultMaximum;
            SetValuePrimAndNumberType();
        }

        private void SetValuePrimAndNumberType()
        {
            AbsoluteMinValuePrim = AbsoluteMinValue;
            AbsoluteMaxValuePrim = AbsoluteMaxValue;
        }

        public void ResetSelectedValues()
        {
            SetSelectedMinValue(AbsoluteMinValue);
            SetSelectedMaxValue(AbsoluteMaxValue);
        }

        /**
         * Returns the absolute minimum value of the range that has been set at construction time.
         *
         * @return The absolute minimum value of the range.
         */

        public float GetAbsoluteMinValue()
        {
            return AbsoluteMinValue;
        }

        /**
         * Returns the absolute maximum value of the range that has been set at construction time.
         *
         * @return The absolute maximum value of the range.
         */

        public float GetAbsoluteMaxValue()
        {
            return AbsoluteMaxValue;
        }

        /**
         * Returns the currently selected min value.
         *
         * @return The currently selected min value.
         */

        public float GetSelectedMinValue()
        {
            return NormalizedToValue(NormalizedMinValue);
        }

        /**
         * Sets the currently selected minimum value. The widget will be Invalidated and redrawn.
         *
         * @param value The Number value to set the minimum value to. Will be clamped to given absolute minimum/maximum range.
         */

        public void SetSelectedMinValue(float value)
        {
            // in case absoluteMinValue == absoluteMaxValue, avoid division by zero when normalizing.
            SetNormalizedMinValue(Math.Abs(AbsoluteMaxValuePrim - AbsoluteMinValuePrim) < float.Epsilon
                ? 0f
                : ValueToNormalized(value));
        }

        /**
         * Returns the currently selected max value.
         *
         * @return The currently selected max value.
         */

        public float GetSelectedMaxValue()
        {
            return NormalizedToValue(NormalizedMaxValue);
        }

        /**
         * Sets the currently selected maximum value. The widget will be Invalidated and redrawn.
         *
         * @param value The Number value to set the maximum value to. Will be clamped to given absolute minimum/maximum range.
         */

        public void SetSelectedMaxValue(float value)
        {
            // in case absoluteMinValue == absoluteMaxValue, avoid division by zero when normalizing.
            SetNormalizedMaxValue(Math.Abs(AbsoluteMaxValuePrim - AbsoluteMinValuePrim) < float.Epsilon
                ? 1f
                : ValueToNormalized(value));
        }

        /**
     * Set the path that defines the shadow of the thumb. This path should be defined assuming
     * that the center of the shadow is at the top left corner (0,0) of the canvas. The
     * {@link #drawThumbShadow(float, Canvas)} method will place the shadow appropriately.
     *
     * @param thumbShadowPath The path defining the thumb shadow
     */

        public void SetThumbShadowPath(Path thumbShadowPath)
        {
            _thumbShadowPath = thumbShadowPath;
        }

        /// <summary>
        ///     Handles thumb selection and movement. Notifies listener callback on certain evs.
        /// </summary>
        public override bool OnTouchEvent(MotionEvent ev)
        {
            if (!Enabled)
            {
                return false;
            }

            int pointerIndex;

            var action = ev.Action;
            switch (action & MotionEventActions.Mask)
            {
                case MotionEventActions.Down:
                    // Remember where the motion ev started
                    _activePointerId = ev.GetPointerId(ev.PointerCount - 1);
                    pointerIndex = ev.FindPointerIndex(_activePointerId);
                    _downMotionX = ev.GetX(pointerIndex);

                    _pressedThumb = EvalPressedThumb(_downMotionX);

                    // Only handle thumb presses.
                    if (_pressedThumb == null)
                    {
                        return base.OnTouchEvent(ev);
                    }

                    Pressed = true;
                    Invalidate();
                    OnStartTrackingTouch();
                    TrackTouchEvent(ev);
                    AttemptClaimDrag();

                    break;
                case MotionEventActions.Move:
                    if (_pressedThumb != null)
                    {
                        if (_isDragging)
                        {
                            TrackTouchEvent(ev);
                        }
                        else
                        {
                            // Scroll to follow the motion ev
                            pointerIndex = ev.FindPointerIndex(_activePointerId);
                            var x = ev.GetX(pointerIndex);

                            if (Math.Abs(x - _downMotionX) > _scaledTouchSlop)
                            {
                                Pressed = true;
                                Invalidate();
                                OnStartTrackingTouch();
                                TrackTouchEvent(ev);
                                AttemptClaimDrag();
                            }
                        }

                        if (NotifyWhileDragging)
                        {
                            if (_pressedThumb == Thumb.Min)
                                OnLowerValueChanged();
                            if (_pressedThumb == Thumb.Max)
                                OnUpperValueChanged();
                        }
                    }
                    break;
                case MotionEventActions.Up:
                    if (_isDragging)
                    {
                        TrackTouchEvent(ev);
                        OnStopTrackingTouch();
                        Pressed = false;
                    }
                    else
                    {
                        // Touch up when we never crossed the touch slop threshold
                        // should be interpreted as a tap-seek to that location.
                        OnStartTrackingTouch();
                        TrackTouchEvent(ev);
                        OnStopTrackingTouch();
                    }

                    _pressedThumb = null;
                    Invalidate();
                    if (_pressedThumb == Thumb.Min)
                        OnLowerValueChanged();
                    if (_pressedThumb == Thumb.Max)
                        OnUpperValueChanged();
                    break;
                case MotionEventActions.PointerDown:
                {
                    var index = ev.PointerCount - 1;
                    // readonly int index = ev.getActionIndex();
                    _downMotionX = ev.GetX(index);
                    _activePointerId = ev.GetPointerId(index);
                    Invalidate();
                    break;
                }
                case MotionEventActions.PointerUp:
                    OnSecondaryPointerUp(ev);
                    Invalidate();
                    break;
                case MotionEventActions.Cancel:
                    if (_isDragging)
                    {
                        OnStopTrackingTouch();
                        Pressed = false;
                    }
                    Invalidate(); // see above explanation
                    break;
            }
            return true;
        }

        private void OnSecondaryPointerUp(MotionEvent ev)
        {
            var pointerIndex = (int) (ev.Action & MotionEventActions.PointerIndexMask) >>
                               (int) MotionEventActions.PointerIndexShift;

            var pointerId = ev.GetPointerId(pointerIndex);
            if (pointerId == _activePointerId)
            {
                // This was our active pointer going up. Choose
                // a new active pointer and adjust accordingly.
                // TODO: Make this decision more intelligent.
                var newPointerIndex = pointerIndex == 0 ? 1 : 0;
                _downMotionX = ev.GetX(newPointerIndex);
                _activePointerId = ev.GetPointerId(newPointerIndex);
            }
        }

        private void TrackTouchEvent(MotionEvent ev)
        {
            var pointerIndex = ev.FindPointerIndex(_activePointerId);
            var x = ev.GetX(pointerIndex);

            if (Thumb.Min.Equals(_pressedThumb) && !_singleThumb)
            {
                SetNormalizedMinValue(ScreenToNormalized(x));
            }
            else if (Thumb.Max.Equals(_pressedThumb))
            {
                SetNormalizedMaxValue(ScreenToNormalized(x));
            }
        }

        /// <summary>
        ///     Tries to claim the user's drag motion, and requests disallowing any ancestors from stealing evs in the drag.
        /// </summary>
        private void AttemptClaimDrag()
        {
            Parent?.RequestDisallowInterceptTouchEvent(true);
        }

        /// <summary>
        ///     This is called when the user has started touching this widget.
        /// </summary>
        private void OnStartTrackingTouch()
        {
            _isDragging = true;
        }

        /// <summary>
        ///     This is called when the user either releases his touch or the touch is canceled.
        /// </summary>
        private void OnStopTrackingTouch()
        {
            _isDragging = false;
        }

        /// <summary>
        ///     Ensures correct size of the widget.
        /// </summary>
        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            var width = 200;
            if (MeasureSpecMode.Unspecified != MeasureSpec.GetMode(widthMeasureSpec))
            {
                width = MeasureSpec.GetSize(widthMeasureSpec);
            }

            var height = _thumbImage.Height
                         + (!_showTextAboveThumbs ? 0 : PixelUtil.DpToPx(Context, HeightInDp))
                         + (_thumbShadow ? _thumbShadowYOffset + _thumbShadowBlur : 0);
            if (MeasureSpecMode.Unspecified != MeasureSpec.GetMode(heightMeasureSpec))
            {
                height = Math.Min(height, MeasureSpec.GetSize(heightMeasureSpec));
            }
            SetMeasuredDimension(width, height);
        }

        /// <summary>
        ///     Draws the widget on the given canvas.
        /// </summary>
        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            _paint.TextSize = _textSize;
            _paint.SetStyle(Paint.Style.Fill);
            _paint.Color = _defaultColor;
            _paint.AntiAlias = true;
            float minMaxLabelSize = 0;

            if (_showLabels)
            {
                // draw min and max labels
                var minLabel = Context.GetString(Resource.String.demo_min_label);
                var maxLabel = Context.GetString(Resource.String.demo_max_label);
                minMaxLabelSize = Math.Max(_paint.MeasureText(minLabel), _paint.MeasureText(maxLabel));
                var minMaxHeight = _textOffset + _thumbHalfHeight + _textSize/3;
                canvas.DrawText(minLabel, 0, minMaxHeight, _paint);
                canvas.DrawText(maxLabel, Width - minMaxLabelSize, minMaxHeight, _paint);
            }
            _padding = _internalPad + minMaxLabelSize + _thumbHalfWidth;

            // draw seek bar background line
            _rect.Left = _padding;
            _rect.Right = Width - _padding;
            canvas.DrawRect(_rect, _paint);

            var selectedValuesAreDefault = NormalizedMinValue <= MinDeltaForDefault &&
                                           NormalizedMaxValue >= 1 - MinDeltaForDefault;

            var colorToUseForButtonsAndHighlightedLine = !_alwaysActive && !_activateOnDefaultValues &&
                                                         selectedValuesAreDefault
                ? _defaultColor
                : // default values
                _activeColor; // non default, filter is active

            // draw seek bar active range line
            _rect.Left = NormalizedToScreen(NormalizedMinValue);
            _rect.Right = NormalizedToScreen(NormalizedMaxValue);

            _paint.Color = colorToUseForButtonsAndHighlightedLine;
            canvas.DrawRect(_rect, _paint);

            // draw minimum thumb (& shadow if requested) if not a single thumb control
            if (!_singleThumb)
            {
                if (_thumbShadow)
                {
                    DrawThumbShadow(NormalizedToScreen(NormalizedMinValue), canvas);
                }
                DrawThumb(NormalizedToScreen(NormalizedMinValue), Thumb.Min.Equals(_pressedThumb), canvas,
                    selectedValuesAreDefault);
            }

            // draw maximum thumb & shadow (if necessary)
            if (_thumbShadow)
            {
                DrawThumbShadow(NormalizedToScreen(NormalizedMaxValue), canvas);
            }
            DrawThumb(NormalizedToScreen(NormalizedMaxValue), Thumb.Max.Equals(_pressedThumb), canvas,
                selectedValuesAreDefault);

            // draw the text if sliders have moved from default edges
            if (!_showTextAboveThumbs || (!_activateOnDefaultValues && selectedValuesAreDefault))
                return;

            _paint.TextSize = _textSize;
            _paint.Color = _textAboveThumbsColor;

            var minText = ValueToString(GetSelectedMinValue());
            var maxText = ValueToString(GetSelectedMaxValue());
            var minTextWidth = _paint.MeasureText(minText);
            var maxTextWidth = _paint.MeasureText(maxText);
            // keep the position so that the labels don't get cut off
            var minPosition = Math.Max(0f, NormalizedToScreen(NormalizedMinValue) - minTextWidth*0.5f);
            var maxPosition = Math.Min(Width - maxTextWidth,
                NormalizedToScreen(NormalizedMaxValue) - maxTextWidth*0.5f);

            if (!_singleThumb)
            {
                // check if the labels overlap, or are too close to each other
                var spacing = PixelUtil.DpToPx(Context, TextLateralPaddingInDp);
                var overlap = minPosition + minTextWidth - maxPosition + spacing;
                if (overlap > 0f)
                {
                    // we could move them the same ("overlap * 0.5f")
                    // but we rather move more the one which is farther from the ends, as it has more space
                    minPosition -= overlap*NormalizedMinValue/(NormalizedMinValue + 1 - NormalizedMaxValue);
                    maxPosition += overlap*(1 - NormalizedMaxValue)/(NormalizedMinValue + 1 - NormalizedMaxValue);
                }
                canvas.DrawText(minText,
                    minPosition,
                    _distanceToTop + _textSize,
                    _paint);
            }

            canvas.DrawText(maxText,
                maxPosition,
                _distanceToTop + _textSize,
                _paint);
        }

        protected string ValueToString(float value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Overridden to save instance state when device orientation changes. This method is called automatically if you assign an id to the RangeSliderView widget using the Id. Other members of this class than the normalized min and max values don't need to be saved.
        /// </summary>
        protected override IParcelable OnSaveInstanceState()
        {
            var bundle = new Bundle();
            bundle.PutParcelable("SUPER", base.OnSaveInstanceState());
            bundle.PutDouble("MIN", NormalizedMinValue);
            bundle.PutDouble("MAX", NormalizedMaxValue);
            return bundle;
        }

        /// <summary>
        ///  Overridden to restore instance state when device orientation changes. This method is called automatically if you assign an id to the RangeSliderView widget using the {@link #setId(int)} method.
        /// </summary>
        protected override void OnRestoreInstanceState(IParcelable parcel)
        {
            var bundle = (Bundle) parcel;
            base.OnRestoreInstanceState((IParcelable) bundle.GetParcelable("SUPER"));
            NormalizedMinValue = bundle.GetFloat("MIN");
            NormalizedMaxValue = bundle.GetFloat("MAX");
        }

        /// <summary>
        /// Draws the "normal" resp. "pressed" thumb image on specified x-coordinate.
        /// </summary>
        /// <param name="screenCoord">The x-coordinate in screen space where to draw the image.</param>
        /// <param name="pressed">Is the thumb currently in "pressed" state?</param>
        /// <param name="canvas">The canvas to draw upon.</param>
        /// <param name="areSelectedValuesDefault"></param>
        private void DrawThumb(float screenCoord, bool pressed, Canvas canvas, bool areSelectedValuesDefault)
        {
            Bitmap buttonToDraw;
            if (!_activateOnDefaultValues && areSelectedValuesDefault)
            {
                buttonToDraw = _thumbDisabledImage;
            }
            else
            {
                buttonToDraw = pressed ? _thumbPressedImage : _thumbImage;
            }

            canvas.DrawBitmap(buttonToDraw, screenCoord - _thumbHalfWidth,
                _textOffset, _paint);
        }

        /// <summary>
        /// Draws a drop shadow beneath the slider thumb.
        /// </summary>
        /// <param name="screenCoord">the x-coordinate of the slider thumb</param>
        /// <param name="canvas">the canvas on which to draw the shadow</param>
        private void DrawThumbShadow(float screenCoord, Canvas canvas)
        {
            _thumbShadowMatrix.SetTranslate(screenCoord + _thumbShadowXOffset,
                _textOffset + _thumbHalfHeight + _thumbShadowYOffset);
            _translatedThumbShadowPath.Set(_thumbShadowPath);
            _translatedThumbShadowPath.Transform(_thumbShadowMatrix);
            canvas.DrawPath(_translatedThumbShadowPath, _shadowPaint);
        }

/**
 * Decides which (if any) thumb is touched by the given x-coordinate.
 *
 * @param touchX The x-coordinate of a touch ev in screen space.
 * @return The pressed thumb or null if none has been touched.
 */

        private Thumb? EvalPressedThumb(float touchX)
        {
            Thumb? result = null;
            var minThumbPressed = IsInThumbRange(touchX, NormalizedMinValue);
            var maxThumbPressed = IsInThumbRange(touchX, NormalizedMaxValue);
            if (minThumbPressed && maxThumbPressed)
            {
                // if both thumbs are pressed (they lie on top of each other), choose the one with more room to drag. this avoids "stalling" the thumbs in a corner, not being able to drag them apart anymore.
                result = touchX/Width > 0.5f ? Thumb.Min : Thumb.Max;
            }
            else if (minThumbPressed)
            {
                result = Thumb.Min;
            }
            else if (maxThumbPressed)
            {
                result = Thumb.Max;
            }
            return result;
        }

        /// <summary>
        /// Decides if given x-coordinate in screen space needs to be interpreted as "within" the normalized thumb x-coordinate.
        /// </summary>
        /// <param name="touchX">The x-coordinate in screen space to check.</param>
        /// <param name="normalizedThumbValue">The normalized x-coordinate of the thumb to check.</param>
        /// <returns>true if x-coordinate is in thumb range, false otherwise.</returns>
        private bool IsInThumbRange(float touchX, float normalizedThumbValue)
        {
            return Math.Abs(touchX - NormalizedToScreen(normalizedThumbValue)) <= _thumbHalfWidth;
        }

/**
 * Sets normalized min value to value so that 0 <= value <= normalized max value <= 1. The View will get Invalidated when calling this method.
 *
 * @param value The new normalized min value to set.
 */

        private void SetNormalizedMinValue(float value)
        {
            NormalizedMinValue = Math.Max(0f, Math.Min(1f, Math.Min(value, NormalizedMaxValue)));
            Invalidate();
        }

/**
 * Sets normalized max value to value so that 0 <= normalized min value <= value <= 1. The View will get Invalidated when calling this method.
 *
 * @param value The new normalized max value to set.
 */

        private void SetNormalizedMaxValue(float value)
        {
            NormalizedMaxValue = Math.Max(0f, Math.Min(1f, Math.Max(value, NormalizedMinValue)));
            Invalidate();
        }

        /// <summary>
        ///     Converts a normalized value to a Number object in the value space between absolute minimum and maximum.
        /// </summary>
        protected float NormalizedToValue(float normalized)
        {
            var v = AbsoluteMinValuePrim + normalized*(AbsoluteMaxValuePrim - AbsoluteMinValuePrim);
            // TODO parameterize this rounding to allow variable decimal points
            return (float) Math.Round(v*100)/100f;
        }

/**
 * Converts the given Number value to a normalized float.
 *
 * @param value The Number value to normalize.
 * @return The normalized float.
 */

        protected float ValueToNormalized(float value)
        {
            if (Math.Abs(AbsoluteMaxValuePrim - AbsoluteMinValuePrim) < float.Epsilon)
            {
                // prev division by zero, simply return 0.
                return 0f;
            }
            return (value - AbsoluteMinValuePrim)/(AbsoluteMaxValuePrim - AbsoluteMinValuePrim);
        }

/**
 * Converts a normalized value into screen space.
 *
 * @param normalizedCoord The normalized value to convert.
 * @return The converted value in screen space.
 */

        private float NormalizedToScreen(float normalizedCoord)
        {
            return _padding + normalizedCoord*(Width - 2*_padding);
        }

/**
 * Converts screen space x-coordinates into normalized values.
 *
 * @param screenCoord The x-coordinate in screen space to convert.
 * @return The normalized value.
 */

        private float ScreenToNormalized(float screenCoord)
        {
            var width = Width;
            if (width <= 2*_padding)
            {
                // prev division by zero, simply return 0.
                return 0f;
            }
            var result = (screenCoord - _padding)/(width - 2*_padding);
            return Math.Min(1f, Math.Max(0f, result));
        }

        public event EventHandler LowerValueChanged;
        public event EventHandler UpperValueChanged;

        protected virtual void OnLowerValueChanged()
        {
            LowerValueChanged?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnUpperValueChanged()
        {
            UpperValueChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Thumb constants (min and max).
        /// </summary>
        private enum Thumb
        {
            Min,
            Max
        }
    }
}