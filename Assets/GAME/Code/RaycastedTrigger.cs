using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastedTrigger : MonoBehaviour {

    #region member variables

    public GameObject m_toDeactivate;

    #endregion

    public void Activate()
    {
        //make the platform disappear
        if (m_toDeactivate != null)
        {
            Destroy(m_toDeactivate.gameObject);
            m_toDeactivate = null;
        }
    }
}
