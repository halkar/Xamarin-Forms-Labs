using System.ComponentModel;
using Xamarin.RangeSeekBar.Forms.UWP;
using XLabs.Forms.Controls;
using XLabs.Forms.WinRT.Shared.Controls.RangeSlider;

#if WINDOWS_APP || WINDOWS_PHONE_APP
using Xamarin.Forms.Platform.WinRT;
#elif WINDOWS_UWP
using Xamarin.Forms.Platform.UWP;
#endif

[assembly: ExportRenderer(typeof(RangeSlider), typeof(RangeSliderRenderer))]

namespace Xamarin.RangeSeekBar.Forms.UWP
{
    public class RangeSliderRenderer : ViewRenderer<RangeSlider, RangeSliderControl>
    {
        protected override void OnElementChanged(ElementChangedEventArgs<RangeSlider> e)
        {
            base.OnElementChanged(e);
            if (Control == null)
            {
                var rangeSeekBar = new RangeSliderControl();
                rangeSeekBar.LowerValueChanged += RangeSeekBar_LowerValueChanged;
                rangeSeekBar.UpperValueChanged += RangeSeekBar_UpperValueChanged;
                SetNativeControl(rangeSeekBar);
            }

            if (Control != null && Element != null)
            {
                Control.Minimum = Element.MinimumValue;
                Control.Maximum = Element.MaximumValue;
                Control.RangeMin = Element.LowerValue;
                Control.RangeMax = Element.UpperValue;
            }
        }

        private void RangeSeekBar_UpperValueChanged(object sender, System.EventArgs e)
        {
            Element.UpperValue = (float)Control.RangeMax;
        }

        private void RangeSeekBar_LowerValueChanged(object sender, System.EventArgs e)
        {
            Element.LowerValue = (float)Control.RangeMin;
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            switch (e.PropertyName)
            {
                case RangeSlider.LowerValuePropertyName:
                    Control.RangeMin = Element.LowerValue;
                    break;
                case RangeSlider.UpperValuePropertyName:
                    Control.RangeMax = Element.UpperValue;
                    break;
                case RangeSlider.MinimumValuePropertyName:
                    Control.Minimum = Element.MinimumValue;
                    break;
                case RangeSlider.MaximumValuePropertyName:
                    Control.Maximum = Element.MaximumValue;
                    break;
            }
        }
    }
}