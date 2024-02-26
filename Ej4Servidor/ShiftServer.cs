using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ej4Servidor
{
    internal class ShiftServer
    {
        static private readonly object l = new object();
        private static Socket servidorSocket;
        static string rutaBin = Environment.GetEnvironmentVariable("HOMEPATH") + "\\Asignaturas\\usuariosPin.bin";
        static string rutaNuevaBin = Environment.GetEnvironmentVariable("HOMEPATH") + "\\Asignaturas\\pin.bin";
        int port = 31416;
        bool puertoOcupado = false;

        String[] users;
        List<String> waitQueue=new List<string>();
        string rutaTxt = Environment.GetEnvironmentVariable("HOMEPATH") + "\\Asignaturas\\usuarios.txt";
        string rutaWaitCola = Environment.GetEnvironmentVariable("HOMEPATH") + "\\Asignaturas\\waitQueu.txt";

        public void ReadNames(String ruta) 
        {
            try 
            {
                using (StreamReader sr = new StreamReader(ruta)) 
                {
                    String allText=sr.ReadToEnd();

                    users=allText.Split(',');
                }
            }
            catch(FileNotFoundException) 
            {
                Console.WriteLine("El archivo no existe");
            }
        }

        public int ReadPin(String ruta) 
        {
            try
            {
                using (BinaryReader br = new BinaryReader(new FileStream(ruta, FileMode.Open)))
                {
                    int pin = br.ReadInt32();
                    if (pin >= 1000 || pin <= 9999)
                    {
                        return pin;
                    }
                    else
                    {
                        Console.WriteLine("El pin no es correcto");
                        return -1;
                    }
                }
            }
            catch (IOException ioEx)
            {
                Console.WriteLine("No se ha podido acceder al archivo");
                return -1;
            }
            catch (Exception ex) 
            {
                Console.WriteLine("Error Inesperado");
                return -1;
            }
        }

        public void cargaWaitQueu() 
        {
            try 
            {
                using (StreamReader sr = new StreamReader(rutaWaitCola)) 
                {
                    string linea="";
                    while ((linea=sr.ReadLine())!=null) 
                    {
                        waitQueue.Add(linea);
                    }
                }
            }
            catch 
            {
                Console.WriteLine("Error al cargar"); 
            }
        } 


        public void Init() 
        {
            int puertoAuziliar = 1024;
            IPEndPoint ie = new IPEndPoint(IPAddress.Any, port);
            using (servidorSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    servidorSocket.Bind(ie);
                    servidorSocket.Listen(10);
                    Console.WriteLine("Servidor Iniciado");
                    puertoOcupado = false;
                    while (true)
                    {
                        Socket client = servidorSocket.Accept();

                        Thread hilo = new Thread(manejoCliente);
                        hilo.Start(client);
                    }
                }
                catch (SocketException e) when
                (e.ErrorCode == (int)SocketError.AddressAlreadyInUse)
                {
                    Console.WriteLine("Puerto en uso");
                    puertoOcupado = true;

                }

            }
            while (puertoOcupado&&port<= 65536)
            {
                port = puertoAuziliar;
                puertoAuziliar++;
            }
        }

        public void manejoCliente(object socket)
        {
            bool admin = false;
            bool user = false;
            bool finaliza=false;
            Socket cliente = (Socket)socket;
            IPEndPoint ieCliente = (IPEndPoint)cliente.RemoteEndPoint;
            cargaWaitQueu();
            ReadNames(rutaTxt);

            using (NetworkStream ns = new NetworkStream(cliente))
            using (StreamReader sr = new StreamReader(ns))
            using (StreamWriter sw = new StreamWriter(ns))
            {
                sw.WriteLine("Binevendio al servidor");
                sw.WriteLine("Escriba su nombre de usuario: ");
                sw.Flush();
                String nombre = sr.ReadLine();
                
                lock (l)
                {
                    if (users.Contains(nombre))
                    {
                        sw.WriteLine("Usuario Conectado con el nombre " + nombre);
                        sw.Flush ();
                        user = true;
                    }
                    else if (nombre == "admin")
                    {
                        sw.WriteLine("Inserte ahora la contraseña del administrador:");
                        sw.Flush();
                        int pin = int.Parse(sr.ReadLine());
                        int pinCorrecto = ReadPin(rutaBin);
                        if (pin == pinCorrecto)
                        {
                            admin = true;
                            sw.WriteLine("Conectado administrador con exito");
                            sw.Flush();
                        }
                        else
                        {
                            finaliza= true;
                            sw.WriteLine("Contraseña incorrecta, Saliendo del servidor...");
                            
                        }
                    }
                    else
                    {
                        finaliza = true;
                        sw.WriteLine("Usuario desconocido");
                        
                    }

                    if (finaliza)
                    {
                        sw.WriteLine("Finalizando servidor...");
                        cliente.Close();
                    }
                    else 
                    {
                        while (!finaliza)
                        {
                            sw.WriteLine("Escriba el comando que desea realizar");
                            sw.Flush();
                            String comandoCliente;
                            comandoCliente = sr.ReadLine();

                            if (admin && !user)
                            {
                                if (comandoCliente.StartsWith("del"))
                                {
                                    String posicion = comandoCliente.Trim().Substring(3);
                                    int pos = int.Parse(posicion);
                                    if (pos <= waitQueue.Count && pos > 0)
                                    {
                                        sw.WriteLine("Se ha eliminado el elemento en la posicion " + pos);
                                        waitQueue.RemoveAt(pos-1);
                                    }
                                    else
                                    {
                                        sw.WriteLine("delete error");

                                    }
                                    sw.Flush();
                                }
                                else if (comandoCliente.StartsWith("chpin"))
                                {
                                    String nuevoPin = comandoCliente.Trim().Substring(5);

                                    if (int.Parse(nuevoPin) > 999)
                                    {
                                        try
                                        {
                                            using (BinaryWriter bw = new BinaryWriter(new FileStream(rutaNuevaBin, FileMode.Create)))
                                            {
                                                bw.Write(nuevoPin);
                                                sw.WriteLine("Se ha guardado la contraseña, ahora es: " + nuevoPin);
                                            }
                                        }
                                        catch
                                        {
                                            sw.WriteLine("El pin no se ha guardado correctamente");
                                        }
                                    }
                                    else
                                    {
                                        sw.WriteLine("Pin no valido");
                                    }
                                }
                                else if (comandoCliente.StartsWith("exit"))
                                {
                                    finaliza = true;
                                }
                                else if (comandoCliente.StartsWith("shutdown"))
                                {
                                    try
                                    {
                                        using (StreamWriter writer = new StreamWriter(rutaWaitCola))
                                        {
                                            foreach (String item in waitQueue)
                                            {
                                                writer.WriteLine(item);
                                            }
                                            sw.WriteLine("Se han añadido al archivo");
                                            sw.Flush();
                                        }
                                    }
                                    catch (FileNotFoundException)
                                    {
                                        sw.WriteLine("Error al guardar");
                                        sw.Flush();
                                    }
                                    finaliza = true;
                                }
                            }

                            if (user || admin)
                            {
                                switch (comandoCliente)
                                {
                                    case "list":
                                        sw.WriteLine("Listado de la lista");
                                        sw.Flush();
                                        foreach (String item in waitQueue)
                                        {
                                            sw.WriteLine(item);
                                            sw.Flush();
                                        }
                                        if (user)
                                        {
                                            finaliza = true;
                                        }
                                        break;
                                    case "add":
                                        waitQueue.Add(nombre);
                                        sw.WriteLine(nombre + " añadido a la lista");
                                        sw.Flush();
                                        if (user)
                                        {
                                            finaliza = true;
                                        }
                                        break;
                                    default:
                                        Console.WriteLine("Comando no aceptado");
                                        break;
                                }
                            }

                        }
                    }
                    
                }
            }
            cliente.Close();
        }
    }
}
