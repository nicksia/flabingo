﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameSparks.Api.Messages;
using GameSparks.Api.Requests;
using GameSparks.Core;
using GameSparks.RT;
using Source.Bingo.Actions;
using Source.Player.Actions;
using UnityEngine;
using UnityEngine.UI;

namespace Source {
	public class GameSparksManager : MonoBehaviour {
		public GameObject GameCanvas;
		public GameObject BlockingMessage;
		public GameObject WinMessage;

		public static GameSparksManager Instance;
	
		private string _challenge;

		private const int STARTING_NUMBERS_CODE = 100;
		private const int NUMBER_CALLED_CODE = 101;
		private const int PLAYER_ID_CODE = 102;
		private const int END_GAME_CODE = 110;

		private const int BINGO_CODE = 201;

		private bool _hacksEnabled;
		private Coroutine _blockerCoroutine;

		void Awake() {
			if (Instance == null) // check to see if the instance has a reference
			{
				Instance = this; // if not, give it a reference to this class...
				DontDestroyOnLoad(this.gameObject); // and make this object persistent as we load new scenes
			} else // if we already have a reference then remove the extra manager from the scene
			{
				Destroy(this.gameObject);
			}
		}

		void Start() {
			Application.targetFrameRate = -1;
			
			StartCoroutine(AuthAsync());
			MatchUpdatedMessage.Listener = message => {
				Debug.Log("Updated Match!");
				message.AddedPlayers.ForEach(p => Debug.Log(p));
			};
		
			MatchFoundMessage.Listener = message => {
				HideBlockingMessage();
				Debug.Log("Found Match: " + message.MatchId);
				message.Participants.ToList().ForEach(p => Debug.Log(p.Id));
				var rt = GetComponent<GameSparksRTUnity>();
				rt.Configure(message,
					peerId => { },
					peerId => { },
					ready => { },
					OnPacketReceived);
				rt.Connect();
				GameCanvas.gameObject.SetActive(true);
			};
		}
		
		
		public void EnableHacks() {
			_hacksEnabled = true;
		}

		public void Bingo() {
			Debug.Log("Pressed Bingo!");
			AudioController.Play("applause4");
			RTData data = new RTData();
			data.SetString(1, "Bingo!");
			GetComponent<GameSparksRTUnity>().SendData(BINGO_CODE, GameSparksRT.DeliveryIntent.RELIABLE, data, new int[]{ 0 });
		}
		

		public void StartMatchmaking() {
			StartCoroutine(StartMatchmakingAsync());
		}

		public void UpdateAccount() {
			new AccountDetailsRequest()
				.Send(response => StateManager.Dispatch(new AccountDetailsResponseAction { AccountDetailsResponse = response } ));
		}

		public void ExitBingoGame(float delay) {
			Debug.Log("Disconnected!");
			GetComponent<GameSparksRTUnity>().Disconnect();
			StartCoroutine(UpdateAccountDelayed(1.5f));
			StartCoroutine(EndGame(delay));
		}

		private void OnPacketReceived(RTPacket packet) {
			switch (packet.OpCode) {
				case STARTING_NUMBERS_CODE:
					Debug.Log($"Got starting numbers: {packet.Data.GetString(1)}");
					Enumerable.Range(0, 3).ToList()
						.ForEach(i => StateManager.Dispatch(new CardStartAction {CardIndex = i, StartingNumbers = packet.Data.GetString((uint) i + 1).Split(',').Select(int.Parse).ToList()}));
					break;
				case NUMBER_CALLED_CODE:
					Debug.Log($"Got numbers: {packet.Data.GetString(1)}");
					List<int> numbersCalled = packet.Data.GetString(1).Split(',').Select(int.Parse).ToList();
					StartCoroutine(PlayPopAsync(numbersCalled[numbersCalled.Count - 1]));
					StateManager.Dispatch(new CalledNumbersUpdateAction {CalledNumbers = numbersCalled});
					break;
				case PLAYER_ID_CODE: 
					Debug.Log($"I am player: {packet.Data.GetString(1)}");
					break;
				case END_GAME_CODE:
					ExitBingoGame(3f);
					break;
			}
			//Debug.Log(packet.OpCode + "-" + packet.Data.ToString());
		}
		
		
		private IEnumerator PlayPopAsync(int i) {
			yield return new WaitForSeconds(0.45f);
			AudioController.Play("new_ball_pops");
			yield return new WaitForSeconds(0.2f);
			AudioController.PlayVoice($"Kendra_{i}");
		}

		private IEnumerator EndGame(float delay) {
			yield return new WaitForSeconds(delay);
			//HideBlockingMessage();
			GameCanvas.gameObject.SetActive(false);
			StateManager.Dispatch(new ResetGameAction());
			WinMessage.SetActive(true);
		}

		private IEnumerator AuthAsync() {
			SetBlockingMessage("Logging in...");
			yield return new WaitForSeconds(0.5f);
			new DeviceAuthenticationRequest().SetDisplayName("Player").Send(response => {
				HideBlockingMessage();
				UpdateAccount();
				StartCoroutine(StartAccountPolling());
				if (!response.HasErrors) {
					Debug.Log($"Authenticated as {response.UserId}");

				} else {
					Debug.Log("Failed to authenticate!");
				}
			});
		}

		private IEnumerator StartAccountPolling() {
			while (true) {
				yield return new WaitForSeconds(10);
				UpdateAccount();
			}
		}

		private IEnumerator UpdateAccountDelayed(float seconds) {
			yield return new WaitForSeconds(seconds);
			UpdateAccount();
		}

		private IEnumerator StartMatchmakingAsync() {
			SetBlockingMessage("Finding a room...");
			new MatchmakingRequest()
				.SetAction(null)
				.SetMatchShortCode("matchClassicBingo")
				.SetSkill(0)
				.Send(response => {
					Debug.Log("Matchmaking Requested");
				});
			yield return null;
		}

		private void SetBlockingMessage(string message) {
			if (_blockerCoroutine != null) {
				StopCoroutine(_blockerCoroutine);
			}

			BlockingMessage.SetActive(true);
			BlockingMessage.GetComponentInChildren<Text>().text = message;
		}

		private void HideBlockingMessage() {
			_blockerCoroutine = StartCoroutine(ScaleOutAsync(BlockingMessage));
		}

		private IEnumerator ScaleOutAsync(GameObject gameObj) {
			Transform firstChild = gameObj.transform.GetChild(0);
			while (firstChild.localScale.y > 0) {
				firstChild.localScale = new Vector3(1, firstChild.localScale.y - 0.09f, 1);
				yield return null;
			}
			
			gameObj.SetActive(false);
			firstChild.transform.localScale = Vector3.one;
		}
	}
}