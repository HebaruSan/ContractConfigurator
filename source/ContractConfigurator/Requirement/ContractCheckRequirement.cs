﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAchievements;
using Contracts;

namespace ContractConfigurator
{
    /// <summary>
    /// ContractRequirement to provide a check against contracts.
    /// </summary>
    public abstract class ContractCheckRequirement : ContractRequirement
    {
        protected string ccType;
        protected Type contractClass;
        protected uint minCount;
        protected uint maxCount;
        protected string tag;

        public override bool LoadFromConfig(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.LoadFromConfig(configNode);

            const string nullString = null; // to get around the fact this is overloaded.
            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "tag", x => tag = x, this, nullString);

            // Get type
            string contractType = null;
            valid &= tag != null || ConfigNodeUtil.ParseValue<string>(configNode, "contractType", x => contractType = x, this);

            // By default, always check the requirement for active contracts
            valid &= ConfigNodeUtil.ParseValue<bool>(configNode, "checkOnActiveContract", x => checkOnActiveContract = x, this, true);

            if (valid)
            {
                if (tag == null)
                    valid &= SetValues(contractType);
                else
                    ccType = null;
            }

            valid &= ConfigNodeUtil.ParseValue<uint>(configNode, "minCount", x => minCount = x, this, 1);
            valid &= ConfigNodeUtil.ParseValue<uint>(configNode, "maxCount", x => maxCount = x, this, UInt32.MaxValue);

            return valid;
        }

        private bool SetValues(string contractType)
        {
            bool valid = true;
            if (ContractType.AllContractTypes.Any(ct => contractType.StartsWith(ct.name)))
            {
                ccType = contractType;
            }
            else
            {
                ccType = null;

                Type type = null;
                ContractConfigurator.contractTypeMap.TryGetValue(contractType, out type);
                if (type == null)
                {
                    valid = false;
                    LoggingUtil.LogError(this, "contractType '{0}' must either be a Contract sub-class or ContractConfigurator contract type", contractType);
                }
                else
                {
                    contractClass = type;
                }
            }
            return valid;
        }

        public override void OnSave(ConfigNode configNode)
        {
            configNode.AddValue("minCount", minCount);
            configNode.AddValue("maxCount", maxCount);
            configNode.AddValue("checkOnActiveContract", checkOnActiveContract);
            if (tag != null)
            {
                configNode.AddValue("tag", tag);
            }
            else
            {
                if (ccType != null)
                {
                    configNode.AddValue("contractType", ccType);
                }
                else if (contractClass != null)
                {
                    configNode.AddValue("contractType", contractClass.Name);
                }
            }
        }

        public override void OnLoad(ConfigNode configNode)
        {
            minCount = ConfigNodeUtil.ParseValue<uint>(configNode, "minCount");
            maxCount = ConfigNodeUtil.ParseValue<uint>(configNode, "maxCount");
            tag = configNode.GetValue("tag");

            if (tag == null)
            {
                string contractType = ConfigNodeUtil.ParseValue<string>(configNode, "contractType");
                SetValues(contractType);
            }
            else
            {
                ccType = null;
                contractClass = null;
            }
        }

        protected string ContractTitle()
        {
            if (tag != null)
            {
                if (KSP.Localization.Localizer.TryGetStringByTag($"#cc.contracttag.OfType.{tag}", out string text))
                    return text;
                return $"of type {tag}";
            }

            string contractTitle;
            if (ccType != null)
            {
                ContractType contractType = ContractType.AllValidContractTypes.Where(ct => ct.name == ccType).FirstOrDefault();
                if (contractType != null)
                {
                    contractTitle = contractType.genericTitle;
                }
                else
                {
                    contractTitle = ccType;
                }
            }
            else
            {
                // TODO - normalize name
                contractTitle = contractClass.Name;
            }

            return contractTitle;
        }
    }
}
