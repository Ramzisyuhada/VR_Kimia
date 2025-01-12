using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MugController : MonoBehaviour
{
    [SerializeField] GameObject Liquid;

    private float diffrence = 25f;
    void Slosh()
    {
        Quaternion inverseRotation = Quaternion.Inverse(transform.localRotation);

        Vector3 finalRotation= Quaternion.RotateTowards(Liquid.transform.localRotation,inverseRotation,60f * Time.deltaTime).eulerAngles;

        finalRotation.x = ClampRotationValue(finalRotation.x,diffrence);
        finalRotation.y = ClampRotationValue(finalRotation.y, diffrence);

        Liquid.transform.localEulerAngles = finalRotation;
    }

    private void MoreSlos()
    {

    }
    private float ClampRotationValue(float value,float diffrence)
    {
        float returnvalue = 0.0f;
        if (value > 180)
        {
            returnvalue = Mathf.Clamp(value, 360 - diffrence, 360);
        }
        else
        {
            returnvalue = Mathf.Clamp(value, 0, diffrence);

        }
        return returnvalue;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Slosh();
    }
}
