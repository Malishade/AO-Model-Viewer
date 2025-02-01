using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts
{
    internal class CatMeshToMonsterData : ScriptableObject
    {
        public Dictionary<int, List<int>> Map;
    }
}