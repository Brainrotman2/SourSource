// Player.cs
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public string playerName;
    public int health = 100;  // Default health
    public List<string> cardLog = new List<string>(); // Log of cards played by the player

    // Player stats
    public int maxHealth = 100;

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health < 0) health = 0;
        Debug.Log(playerName + " took " + damage + " damage. Current health: " + health);
    }

    public void Heal(int healingAmount)
    {
        health += healingAmount;
        if (health > maxHealth) health = maxHealth;
        Debug.Log(playerName + " healed " + healingAmount + " points. Current health: " + health);
    }

    public void AddToCardLog(string cardName)
    {
        cardLog.Add(cardName);
        Debug.Log(playerName + " played: " + cardName);
    }
}