using System;

namespace App.Models
{
    /// <summary>
    /// Univerzální obálka pro odesílání dat z Unity na Backend.
    /// T = Typ dat, která se reálnì posílají (napø. pozice, skóre, eventy).
    /// </summary>
    [Serializable]
    public class GameMessage<T>
    {
        // Razítka, která Backend potøebuje k uložení do správné tabulky
        public int session_id;
        public int iteration; // Aktuální kolo (iterace)
        public string topic;  // Název kanálu, aby i v DB bylo vidìt, o co jde

        // Samotný obsah zprávy (generický typ T)
        public T data;

        // Konstruktor pro snadné vytváøení v SessionManageru
        public GameMessage(int sessionId, int iter, string top, T payloadData)
        {
            this.session_id = sessionId;
            this.iteration = iter;
            this.topic = top;
            this.data = payloadData;
        }
    }
}