using UnityEngine;
using UnityEngine.UIElements;

public class MainView : MonoBehaviour
{
    [SerializeField]
    private ModelViewer _modelViewer;

    [SerializeField]
    private UIDocument _uiDocument;

    void OnEnable() => new MainViewUxml(_uiDocument.rootVisualElement, _modelViewer);
}