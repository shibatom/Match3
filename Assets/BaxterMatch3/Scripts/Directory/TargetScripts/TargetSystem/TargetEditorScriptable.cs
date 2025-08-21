

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Internal.Scripts.TargetScripts.TargetSystem
{
    /// <summary>
    /// Target editor
    /// </summary>
    public class TargetEditorScriptable : ScriptableObject
    {
        public List<TargetContainer> targets = new List<TargetContainer>();

        public TargetContainer GetTargetByName(string getTargetsName) => targets.First(i => i.name == getTargetsName);
    }
}