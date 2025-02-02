﻿using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GreenBar : MonoBehaviour
{
    public Slider slider;
    [SerializeField] private int barMax = 0;
    [SerializeField] private int curVal = 0;

    private void Awake() {
        slider.maxValue = barMax;
        slider.value = curVal;
    }
    public void SetMaxBarValue(int value){
        barMax = value;
        slider.maxValue = barMax;
    }
    public void SetGreenBarValue(int value){
        curVal = value;
        slider.value = curVal;
    }

    public void ChangeValue(int value){
        if (IsGrrenBarWithinBounds(curVal + value)){
            curVal += value;
            slider.value = curVal;
        }
    }

    private bool IsGrrenBarWithinBounds(int val){
        if (val > barMax || val < 0){
            return false;
        } else {
            return true;
        }
    }
}
