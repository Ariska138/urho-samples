﻿using System.Collections.Generic;
using System.Threading.Tasks;
using ShootySkies.Aircrafts.Enemies;
using Urho;

namespace ShootySkies
{
	public class ShootySkiesGame : Application
	{
		const string CoinstFormat = "{0} coins";

		int coins;
		Scene scene;
		Text coinsText;
		List<Enemy> enemies;

		public Player Player { get; private set; }

		public Viewport Viewport { get; private set; }

		public ShootySkiesGame(Context c) : base(c, new ApplicationOptions { Height = 800, Width = 500, Orientation = ApplicationOptions.OrientationType.Portrait }) { }

		public override void Start()
		{
			base.Start();
			CreateScene();
			SubscribeToKeyDown(e => { if (e.Key == Key.Esc) Engine.Exit(); });
		}

		async void CreateScene()
		{
			scene = new Scene(Context);
			scene.CreateComponent<Octree>();

			var physics = scene.CreateComponent<PhysicsWorld>();
			physics.SetGravity(new Vector3(0, 0, 0));

			// Camera
			var cameraNode = scene.CreateChild();
			cameraNode.Position = (new Vector3(0.0f, 0.0f, -10.0f));
			cameraNode.CreateComponent<Camera>();
			Renderer.SetViewport(0, Viewport = new Viewport(Context, scene, cameraNode.GetComponent<Camera>(), null));

			// UI
			coinsText = new Text(Context);
			coinsText.HorizontalAlignment = HorizontalAlignment.Right;
			coinsText.SetFont(ResourceCache.GetFont(Assets.Fonts.Font), Graphics.Width / 20);
			UI.Root.AddChild(coinsText);
			Input.SetMouseVisible(true, false);

			// Background
			var background = new Background(Context);
			scene.AddComponent(background);
			background.Start();

			// Lights:
			var lightNode1 = scene.CreateChild();
			lightNode1.Position = new Vector3(0, -5, -40);
			lightNode1.AddComponent(new Light(Context) { LightType = LightType.Point, Range = 120, Brightness = 1.5f });

			var lightNode2 = scene.CreateChild();
			lightNode2.Position = new Vector3(10, 15, -12);
			lightNode2.AddComponent(new Light(Context) { LightType = LightType.Point, Range = 30.0f, CastShadows = true, Brightness = 1.5f });

			// Menu
			var startMenu = new StartMenu(Context);
			scene.AddComponent(startMenu);

			// Game logic cycle
			while (true)
			{
				await startMenu.ShowStartMenu(); //wait for "start"
				await StartGame();
			}
		}

		async Task StartGame()
		{
			UpdateCoins(0);
			Player = new Player(Context);
			var aircraftNode = scene.CreateChild(nameof(Aircraft));
			aircraftNode.AddComponent(Player);
			var playersLife = Player.Play();
			Enemies enemies = new Enemies(Context, Player);
			scene.AddComponent(enemies);
			SpawnCoins();
			enemies.StartSpawning();
			await playersLife;
			enemies.KillAll();
			aircraftNode.Remove();
		}
		
		async void SpawnCoins()
		{
			while (Player.IsAlive)
			{
				var coinNode = scene.CreateChild();
				coinNode.Position = new Vector3(RandomHelper.NextRandom(-2.5f, 2.5f), 4f, 0);
				var coin = new Coin(Context);
				coinNode.AddComponent(coin);
				await Task.WhenAll(coin.FireAsync(false), coinNode.RunActionsAsync(new DelayTime(10f)));
				coinNode.Remove();
			}
		}

		public void OnCoinCollected() => UpdateCoins(coins + 1);

		void UpdateCoins(int amount)
		{
			if (amount == 5)
			{
				// give player a MassMachineGun once he earns 5 coins
				Player.Node.AddComponent(new MassMachineGun(Context));
			}
			coins = amount;
			coinsText.Value = string.Format(CoinstFormat, coins);
		}
	}
}
