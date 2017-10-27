using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Data.Validators;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Applications.ContentManager.ReturnFieldEditorValues;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using System;

namespace Sitecore.Support.Shell.Applications.ContentManager.ReturnFieldEditorValues
{
    public class Validate
    {
        public void Process(ReturnFieldEditorValuesArgs args)
        {
            this.ProcessInternal(args);
        }

        protected void ProcessInternal(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.IsPostBack)
            {
                if (args.Result == "no")
                {
                    args.AbortPipeline();
                }
                args.IsPostBack = false;
                return;
            }
            string formValue = WebUtil.GetFormValue("scValidatorsKey");
            if (!string.IsNullOrEmpty(formValue))
            {
                ValidatorCollection validators = ValidatorManager.GetValidators(ValidatorsMode.ValidatorBar, formValue);
                ValidatorOptions options = new ValidatorOptions(true);
                ValidatorManager.Validate(validators, options);
                bool skipFatalErrorsForStandardValues = true;
                if (validators.Count > 0)
                {
                    Item item = Client.ContentDatabase.GetItem(validators[0].ItemUri.ToDataUri());
                    if (item != null)
                    {
                        string setting = Settings.GetSetting("ParametersTemplateRoots", "templates/System/Layout/Rendering Parameters");
                        string[] array = setting.Split(new char[]
                        {
                            '|'
                        });
                        for (int i = 0; i < array.Length; i++)
                        {
                            string text = array[i];
                            if (item.Paths.FullPath.ToLower().Contains(text.ToLower()))
                            {
                                skipFatalErrorsForStandardValues = false;
                                break;
                            }
                        }
                    }
                }
                Pair<ValidatorResult, BaseValidator> strongestResult = ValidatorManager.GetStrongestResult(validators, true, skipFatalErrorsForStandardValues);
                ValidatorResult part = strongestResult.Part1;
                BaseValidator part2 = strongestResult.Part2;
                if (part2 != null && part2.IsEvaluating)
                {
                    SheerResponse.Alert("The fields in this item have not been validated.\n\nWait until validation has been completed and then save your changes.", new string[0]);
                    args.AbortPipeline();
                    return;
                }
                switch (part)
                {
                    case ValidatorResult.CriticalError:
                        {
                            string text2 = Translate.Text("Some of the fields in this item contain critical errors.\n\nAre you sure you want to save this item111?");
                            if (MainUtil.GetBool(args.CustomData["showvalidationdetails"], false) && part2 != null)
                            {
                                text2 += ValidatorManager.GetValidationErrorDetails(part2);
                            }
                            SheerResponse.Confirm(text2);
                            args.WaitForPostBack();
                            return;
                        }
                    case ValidatorResult.FatalError:
                        {
                            string text3 = Translate.Text("Some of the fields in this item contain fatal errors.\n\nYou must resolve these errors before you can save this item.");
                            if (MainUtil.GetBool(args.CustomData["showvalidationdetails"], false) && part2 != null)
                            {
                                text3 += ValidatorManager.GetValidationErrorDetails(part2);
                            }
                            SheerResponse.Alert(text3, new string[0]);
                            args.AbortPipeline();
                            break;
                        }
                    default:
                        return;
                }
            }
        }
    }
}
