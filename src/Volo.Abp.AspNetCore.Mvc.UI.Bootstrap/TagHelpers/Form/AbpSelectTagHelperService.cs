﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using Localization.Resources.AbpUi;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Localization;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.Microsoft.AspNetCore.Razor.TagHelpers;

namespace Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form
{
    public class AbpSelectTagHelperService : AbpTagHelperService<AbpSelectTagHelper>
    {
        private readonly IHtmlGenerator _generator;
        private readonly HtmlEncoder _encoder;
        private readonly IStringLocalizer<AbpUiResource> _localizer;

        public AbpSelectTagHelperService(IHtmlGenerator generator, HtmlEncoder encoder, IStringLocalizer<AbpUiResource> localizer)
        {
            _generator = generator;
            _encoder = encoder;
            _localizer = localizer;
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var innerHtml = GetFormInputGroupAsHtml(context, output);

            var order = GetInputOrder(TagHelper.AspFor.ModelExplorer);

            AddGroupToFormGroupContents(context, TagHelper.AspFor.Name, SurroundInnerHtmlAndGet(innerHtml), order, out var surpress);

            if (surpress)
            {
                output.SuppressOutput();
            }
            else
            {
                output.TagName = "div";
                output.Attributes.AddClass("form-group");
                output.TagMode = TagMode.StartTagAndEndTag;
                output.Content.SetHtmlContent(innerHtml);
            }
        }

        protected virtual string GetFormInputGroupAsHtml(TagHelperContext context, TagHelperOutput output)
        {
            var selectTag = GetSelectTag(context);
            var selectAsHtml = RenderTagHelperOutput(selectTag, _encoder);
            var label = GetLabelAsHtml(selectTag);

            return label + Environment.NewLine + selectAsHtml;
        }

        protected virtual string SurroundInnerHtmlAndGet(string innerHtml)
        {
            return "<div class=\"form-group\">" + Environment.NewLine + innerHtml + Environment.NewLine + "</div>";
        }

        protected virtual TagHelperOutput GetSelectTag(TagHelperContext context)
        {
            var selectItems = TagHelper.AspItems?.ToList();

            if (TagHelper.AspItems == null && !GetSelectItemsIfProvidedByEnum(TagHelper.AspFor.ModelExplorer, out selectItems) && !GetSelectItemsIfProvidedFromAttribute(TagHelper.AspFor.ModelExplorer, out selectItems))
            {
                throw new Exception("No items provided for select attribute.");
            }

            SetSelectedValue(selectItems);

            var selectTagHelper = new SelectTagHelper(_generator)
            {
                For = TagHelper.AspFor,
                Items = selectItems,
                ViewContext = TagHelper.ViewContext
            };

            var inputTagHelperOutput = GetInnerTagHelper(new TagHelperAttributeList(), context, selectTagHelper, "select", TagMode.StartTagAndEndTag); ;

            inputTagHelperOutput.Attributes.Add("class", "form-control");

            return inputTagHelperOutput;
        }

        protected virtual string GetLabelAsHtml(TagHelperOutput selectTag)
        {
            if (string.IsNullOrEmpty(TagHelper.Label) && string.IsNullOrEmpty(TagHelper.AspFor.Metadata.DisplayName))
            {
                return "";
            }

            return "<label " + GetIdAttributeAsString(selectTag) + ">" + _localizer[GetLabelText()] + "</label>";
        }

        protected virtual string GetLabelText()
        {
            return string.IsNullOrEmpty(TagHelper.Label) ?
                TagHelper.AspFor.Metadata.DisplayName :
                TagHelper.Label;
        }

        protected virtual bool GetSelectItemsIfProvidedByEnum(ModelExplorer explorer, out List<SelectListItem> selectItems)
        {
            selectItems = explorer.Metadata.IsEnum ? explorer.ModelType.GetTypeInfo().GetMembers(BindingFlags.Public | BindingFlags.Static)
                .Select((t, i) => new SelectListItem { Value = i.ToString(), Text = t.Name }).ToList() : null;

            return selectItems != null;
        }

        protected virtual bool GetSelectItemsIfProvidedFromAttribute(ModelExplorer explorer, out List<SelectListItem> selectItems)
        {
            selectItems = GetAttribute<SelectItems>(explorer)?.GetItems(explorer, explorer.Model.ToString())?.ToList();

            return selectItems != null;
        }

        protected virtual void SetSelectedValue(List<SelectListItem> selectItems)
        {
            var selectedValue = GetSelectedValue();

            if (!selectItems.Any(si => si.Selected))
            {
                var itemToBeSelected = selectItems.FirstOrDefault(si => si.Value.ToString() == selectedValue);

                if (itemToBeSelected != null)
                {
                    itemToBeSelected.Selected = true;
                }
            }
        }

        protected virtual string GetSelectedValue()
        {
            if (TagHelper.AspFor.ModelExplorer.Metadata.IsEnum)
            {
                var baseType = TagHelper.AspFor.ModelExplorer.Model.GetType().GetEnumUnderlyingType();
                var valueAsString = Convert.ChangeType(TagHelper.AspFor.ModelExplorer.Model, baseType);
                return valueAsString != null ? valueAsString.ToString() : "";
            }

            return TagHelper.AspFor.ModelExplorer.Model.ToString();
        }
    }
}