using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static AODB.Common.DbClasses.RDBMesh_t.FAFAnim_t;

public class UVAnimation : MonoBehaviour
{
    private MeshRenderer _meshRenderer;
    private UVKey[] _keys;
    private int _currentKey = 0;
    private float _time;

    public void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    public void Init(UVKey[] keys)
    {
        _keys = keys;
    }

    public void Update()
    {
        float currentFrameDuration = _keys[_currentKey + 1].Time - _keys[_currentKey].Time;
        if (_time > currentFrameDuration)
        {
            _currentKey++;
            _time = 0;

            if (_currentKey == _keys.Length - 1)
                _currentKey = 0;
        }

        float xOffset = Mathf.Lerp(_keys[_currentKey].Offset.X, _keys[_currentKey + 1].Offset.X, _time / currentFrameDuration);
        float yOffset = Mathf.Lerp(_keys[_currentKey].Offset.Y, _keys[_currentKey + 1].Offset.Y, _time / currentFrameDuration);

        _meshRenderer.material.mainTextureOffset = new Vector2(xOffset, yOffset);

        _time += Time.deltaTime;
    }
}
