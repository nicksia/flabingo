﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Source.Bingo.Views {
    public class ReferenceNumbersView : MonoBehaviour {
        private void OnEnable() {
            StateManager.SubscribeUntilDisable(this, state => {
                LoadNumbers(state.Bingo.CalledNumbers);
            });
        }
        
        private void LoadNumbers(List<int> numbers) {
            if (numbers == null) {
                return;
            }
            
            for (int i = 0; i < transform.childCount && i < numbers.Count; i++) {
                transform.GetChild(numbers[i] - 1).GetComponentInChildren<Button>().interactable = false;
            }
        }
    }
}
