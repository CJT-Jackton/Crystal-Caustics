using UnityEngine;

public class Rainbow : MonoBehaviour
{
    public float Speed;

    private Color _color;
    private Color _transmittanceColor;
    private float _opacity;

    // Start is called before the first frame update
    void Start()
    {
        _color = GetComponent<Renderer>().material.GetColor("_BaseColor");
        _transmittanceColor = GetComponent<Renderer>().material.GetColor("_TransmittanceColor");
        _opacity = _color.a;
    }

    // Update is called once per frame
    void Update()
    {
        float H, S, V;

        _color = GetComponent<Renderer>().material.GetColor("_BaseColor");
        Color.RGBToHSV(_color, out H, out S, out V);
        H += Speed * Time.deltaTime;
        _color = Color.HSVToRGB(H, S, V);
        _color.a = _opacity;
        GetComponent<Renderer>().material.SetColor("_BaseColor", _color);


        _transmittanceColor = GetComponent<Renderer>().material.GetColor("_TransmittanceColor");
        Color.RGBToHSV(_transmittanceColor, out H, out S, out V);
        H += Speed * Time.deltaTime;
        _transmittanceColor = Color.HSVToRGB(H, S, V);
        GetComponent<Renderer>().material.SetColor("_TransmittanceColor", _transmittanceColor);
    }
}
