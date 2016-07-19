// ***********************************************************************
// Assembly         : XLabs.Forms
// Author           : Artur Shamsutdinov
// Created          : 12-27-2015
// 
// Last Modified By : XLabs Team
// Last Modified On : 01-04-2016
// ***********************************************************************
// <copyright file="RangeSlider.cs" company="XLabs Team">
//     Copyright (c) XLabs Team. All rights reserved.
// </copyright>
// <summary>
//       This project is licensed under the Apache 2.0 license
//       https://github.com/XLabs/Xamarin-Forms-Labs/blob/master/LICENSE
//       
//       XLabs is a open source project that aims to provide a powerfull and cross 
//       platform set of controls tailored to work with Xamarin Forms.
// </summary>
// ***********************************************************************
// 


using System;
using Xamarin.Forms;

namespace XLabs.Forms.Controls
{
    public class RangeSlider : View
    {
        public const string LowerValuePropertyName = "LowerValue";
        public const string MaximumValuePropertyName = "MaximumValue";
        public const string MinimumValuePropertyName = "MinimumValue";
        public const string UpperValuePropertyName = "UpperValue";

        public readonly BindableProperty LowerValueProperty =
            BindableProperty.Create(LowerValuePropertyName, typeof(float), typeof(RangeSlider), 0f);

        public readonly BindableProperty MaximumValueProperty =
            BindableProperty.Create(MaximumValuePropertyName, typeof(float), typeof(RangeSlider), 1f);

        public readonly BindableProperty MinimumValueProperty =
            BindableProperty.Create(MinimumValuePropertyName, typeof(float), typeof(RangeSlider), 0f);

        public readonly BindableProperty UpperValueProperty =
            BindableProperty.Create(UpperValuePropertyName, typeof(float), typeof(RangeSlider), 1f);

        public float MinimumValue
        {
            get { return (float) GetValue(MinimumValueProperty); }
            set { SetValue(MinimumValueProperty, value); }
        }

        public float MaximumValue
        {
            get { return (float) GetValue(MaximumValueProperty); }
            set { SetValue(MaximumValueProperty, value); }
        }

        public float LowerValue
        {
            get { return (float) GetValue(LowerValueProperty); }
            set { SetValue(LowerValueProperty, value); }
        }

        public float UpperValue
        {
            get { return (float) GetValue(UpperValueProperty); }
            set { SetValue(UpperValueProperty, value); }
        }

        public event EventHandler LowerValueChanged;
        public event EventHandler UpperValueChanged;

        public void OnLowerValueChanged(float value)
        {
            LowerValue = value;
            LowerValueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void OnUpperValueChanged(float value)
        {
            UpperValue = value;
            UpperValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}