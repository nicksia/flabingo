﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameSparks.Api.Messages;
using GameSparks.Api.Requests;
using GameSparks.Core;
using GameSparks.RT;
using UnityEngine;

public class GameSparksManager : MonoBehaviour {
	private static GameSparksManager instance = null;
	private string _challenge;

	void Awake() {
		if (instance == null) // check to see if the instance has a reference
		{
			instance = this; // if not, give it a reference to this class...
			DontDestroyOnLoad(this.gameObject); // and make this object persistent as we load new scenes
		} else // if we already have a reference then remove the extra manager from the scene
		{
			Destroy(this.gameObject);
		}
	}

	void Start() {
		StartCoroutine(AuthAsync());
		MatchUpdatedMessage.Listener = message => {
			Debug.Log("Updated Match!");
			message.AddedPlayers.ForEach(p => Debug.Log(p));
		};
		
		MatchFoundMessage.Listener = message => {
			Debug.Log("Found Match: " + message.MatchId);
			message.Participants.ToList().ForEach(p => Debug.Log(p.Id));
			var rt = gameObject.AddComponent<GameSparksRTUnity>();
			rt.Configure(message,
				peerId => { },
				peerId => { },
				ready => { },
				OnPacketReceived);
			rt.Connect();
		};

		ChallengeStartedMessage.Listener = message => {
			Debug.Log("Started:" + message.Challenge.ChallengeName);
		};
	}

	private void OnPacketReceived(RTPacket packet) {
		Debug.Log(packet.Data.ToString());
	}


	private IEnumerator StartUpdateLoopAsync(string challengeInstanceId) {
		yield return new WaitForSeconds(1f);
		Debug.Log("Ping...");
		new GetChallengeRequest()
			.SetChallengeInstanceId(challengeInstanceId)
			.Send(response => {
				Debug.Log(response.Challenge.ScriptData.JSON);

				StartCoroutine(StartUpdateLoopAsync(challengeInstanceId));
			});
		
		Debug.Log("Pong...");
		new LogChallengeEventRequest()
			.SetChallengeInstanceId(challengeInstanceId)
			.SetEventKey("action_testEvent")
			.Send(response => {
				Debug.Log("ZORK!");
			});
	}

	private IEnumerator AuthAsync() {
		yield return new WaitForSeconds(1f);
		new DeviceAuthenticationRequest().SetDisplayName("Nick Sia").Send(response => {
			if (!response.HasErrors) {
				Debug.Log($"Authenticated as {response.UserId}");
				StartCoroutine(MatchMakeAsync());
			} else {
				Debug.Log("Failed to authenticate!");
			}
		});
	}

	private IEnumerator MatchMakeAsync() {
		
		new MatchmakingRequest()
			.SetAction(null)
			.SetMatchShortCode("matchClassicBingo")
			.SetSkill(0)
			.Send(response => {
				Debug.Log("Matchmaking Requested");
			});
		yield return null;
		
	}
}

//.SetDisplayName("Nick Sia")
//.SetPassword("password")
//.SetUserName("Nick Sia")