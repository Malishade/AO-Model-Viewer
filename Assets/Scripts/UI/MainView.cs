using UnityEngine;
using UnityEngine.UIElements;

public class MainView : MonoBehaviour
{
    [SerializeField]
    private VisualTreeAsset _listViewEntryTemplate;

    [SerializeField]
    private ModelViewer _modelViewer;

    [SerializeField]
    private UIDocument _uiDocument;

    [SerializeField]
    private Camera _renderCamera;

    void OnEnable() => new MainViewUxml(_uiDocument.rootVisualElement, _modelViewer, _renderCamera);
}