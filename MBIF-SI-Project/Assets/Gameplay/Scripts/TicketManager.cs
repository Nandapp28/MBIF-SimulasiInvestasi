using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class TicketManager : MonoBehaviour
{
    private List<int> tickets = new List<int>();

    public void InitializeTickets(int totalPlayers)
    {
        tickets.Clear();
        for (int i = 1; i <= totalPlayers; i++)
        {
            tickets.Add(i);
        }
    }

    public int PickTicketForPlayer(int chosenTicket)
    {
        if (tickets.Contains(chosenTicket))
        {
            tickets.Remove(chosenTicket);
            return chosenTicket;
        }
        else
        {
            Debug.LogError("Ticket not available!");
            return -1;
        }
    }
public static void ShuffleList<T>(List<T> list)
{
    for (int i = 0; i < list.Count; i++)
    {
        int randomIndex = UnityEngine.Random.Range(i, list.Count);
        T temp = list[i];
        list[i] = list[randomIndex];
        list[randomIndex] = temp;
    }
}
    public int GetRandomTicketForBot()
    {
        if (tickets.Count == 0) return -1;

        int randomIndex = Random.Range(0, tickets.Count);
        int ticket = tickets[randomIndex];
        tickets.RemoveAt(randomIndex);
        return ticket;
    }
}
