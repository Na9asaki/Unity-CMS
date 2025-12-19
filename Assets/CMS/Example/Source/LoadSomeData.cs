using System;
using CMS;
using UnityEngine;

namespace Source.Example
{
    public class LoadSomeData : MonoBehaviour
    {
        private void Start()
        {
            CMSEntry.Launch();
            var result = CMSEntry.CMSDataCollection.GetBy<ExampleData2>();
            Debug.Log(result.damage);
            Debug.Log(result.clipCapacity);
            Debug.Log(result.fireRate);
        }
    }
    
}