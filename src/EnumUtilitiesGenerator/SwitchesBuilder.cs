using System;
using System.Collections.Generic;
using System.Linq;

namespace EnumUtilitiesGenerator
{
    internal class SwitchesBuilder
    {
        private const string DEFAULT_CASE = "_";
        private readonly string _format;
        private readonly Dictionary<string, (string ReturnValue, bool IsDuplicate)> _switchCases = new(StringComparer.InvariantCultureIgnoreCase);
        private bool _hasDefault;
        private (string Default, string Value) _defaultCase;

        public SwitchesBuilder(string format)
        {
            _format = format;
        }

        public void Add(string caseValue, string returnValue)
        {
            if (caseValue == DEFAULT_CASE)
            {
                if (!_hasDefault)
                {
                    _hasDefault = true;
                    _defaultCase = (caseValue, returnValue);
                    return;
                }
                else
                    return;
            }

            if (_switchCases.TryGetValue(caseValue, out var value) && !value.IsDuplicate)
            {
                const string exceptionTemplate = "throw new System.InvalidOperationException($\"Multiple members were found with description '{description}'. Could not map description.\")";
                _switchCases[caseValue] = (exceptionTemplate, true);
                return;
            }

            _switchCases[caseValue] = (returnValue, false);
        }

        public string Build(string indentation)
        {
            var switches = _switchCases.Select(x => string.Format($"{indentation}{_format}", x.Key, x.Value.ReturnValue)).ToList();

            if (_hasDefault)
                switches.Add($"{indentation}{_defaultCase.Default} => {_defaultCase.Value}");

            return string.Join(",\r\n", switches);
        }
    }
}
