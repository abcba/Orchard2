using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Fluid;
using Fluid.Values;
using Microsoft.AspNetCore.Mvc.Localization;

using OrchardCore.Mvc.Utilities;

namespace OrchardCore.DisplayManagement.Liquid.Filters
{
    public static class LiquidViewFilters
    {
        private static readonly AsyncFilterDelegate _localizeDelegate = Localize;
        private static readonly AsyncFilterDelegate _htmlClassDelegate = HtmlClass;
        private static readonly AsyncFilterDelegate _newShapeDelegate = NewShape;
        private static readonly AsyncFilterDelegate _shapeRenderDelegate = ShapeRender;
        private static readonly AsyncFilterDelegate _shapeStringifyDelegate = ShapeStringify;
        private static readonly FilterDelegate _shapePropertiesDelegate = ShapeProperties;

        public static FilterCollection WithLiquidViewFilters(this FilterCollection filters)
        {
            filters.AddAsyncFilter("t", _localizeDelegate);
            filters.AddAsyncFilter("html_class", _htmlClassDelegate);
            filters.AddAsyncFilter("shape_new", _newShapeDelegate);
            filters.AddAsyncFilter("shape_render", _shapeRenderDelegate);
            filters.AddAsyncFilter("shape_stringify", _shapeStringifyDelegate);
            filters.AddFilter("shape_properties", _shapePropertiesDelegate);

            return filters;
        }

        public static ValueTask<FluidValue> Localize(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (!context.AmbientValues.TryGetValue("ViewLocalizer", out var item) || !(item is IViewLocalizer localizer))
            {
                return ThrowArgumentException<ValueTask<FluidValue>>("ViewLocalizer missing while invoking 't'");
            }

            var parameters = new object[arguments.Count];
            for (var i = 0; i < arguments.Count; i++)
            {
                parameters[i] = arguments.At(i).ToStringValue();
            }

            return new ValueTask<FluidValue>(new StringValue(localizer.GetString(input.ToStringValue(), parameters)));
        }

        public static ValueTask<FluidValue> HtmlClass(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            return new ValueTask<FluidValue>(new StringValue(input.ToStringValue().HtmlClassify()));
        }

        public static async ValueTask<FluidValue> NewShape(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (!context.AmbientValues.TryGetValue("ShapeFactory", out var item) || !(item is IShapeFactory shapeFactory))
            {
                return ThrowArgumentException<FluidValue>("ShapeFactory missing while invoking 'shape_new'");
            }

            var type = input.ToStringValue();
            var properties = new Dictionary<string, object>(arguments.Count);

            foreach (var name in arguments.Names)
            {
                properties.Add(name.ToPascalCaseUnderscore(), arguments[name].ToObjectValue());
            }

            return FluidValue.Create(await shapeFactory.CreateAsync(type, Arguments.From(properties)));
        }

        public static async ValueTask<FluidValue> ShapeStringify(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (input.ToObjectValue() is IShape shape)
            {
                if (!context.AmbientValues.TryGetValue("DisplayHelper", out var item) || !(item is IDisplayHelper displayHelper))
                {
                    return ThrowArgumentException<FluidValue>("DisplayHelper missing while invoking 'shape_stringify'");
                }

                return new HtmlContentValue(await displayHelper.ShapeExecuteAsync(shape));
            }

            return NilValue.Instance;
        }

        public static async ValueTask<FluidValue> ShapeRender(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (input.ToObjectValue() is IShape shape)
            {
                if (!context.AmbientValues.TryGetValue("DisplayHelper", out var item) || !(item is IDisplayHelper displayHelper))
                {
                    return ThrowArgumentException<FluidValue>("DisplayHelper missing while invoking 'shape_render'");
                }

                return new HtmlContentValue(await displayHelper.ShapeExecuteAsync(shape));
            }

            return NilValue.Instance;
        }

        public static FluidValue ShapeProperties(FluidValue input, FilterArguments arguments, TemplateContext context)
        {
            if (input.ToObjectValue() is IShape shape)
            {
                foreach (var name in arguments.Names)
                {
                    shape.Properties[name.ToPascalCaseUnderscore()] = arguments[name].ToObjectValue();
                }
                return FluidValue.Create(shape);
            }

            return NilValue.Instance;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T ThrowArgumentException<T>(string message)
        {
            throw new ArgumentException(message);
        }
    }
}
