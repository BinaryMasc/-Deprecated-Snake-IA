using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp2
{
    public partial class Form1 : Form
    {
        
        public static bool debugmode = false;
        public static bool mostrarGrafics = false;
        public static bool guardarmodelos = false;

        bool pausa = false;



        public const int indexHilos = 20;
        public const int IndexAgentes = 20;      //  Demanda de procesamiento en todos los núcleos físicos y lógicos directamente proporcional a Hilos (Agentes = Hilos)
        int delta = 1;                        //  Demanda de procesamiento en todos los núcleos físicos y lógicos inversamente proporcional a delta
        public static int cuadro = 10;
        int GameMode = 2;
        int c = 0;
        static int generacion = 1;
        int agenteEjecutandose;
        double promedioPuntaje = 0;


        int indiceMejorPadre = 0;
        int indicePeorHijo1 = 0;
        int indicePeorHijo2 = 0;

        bool primeraIteracion = true;
        static bool haySobrevivientes = false;

        public static string estadisticasPath = @"C:\Users\" + Environment.UserName + @"\Desktop\statistics.dat";

        //Cola cabeza;
        //Comida comida;

        public static bool[] IagentesVivos = new bool[IndexAgentes];

        public static List<double> registroGraf1 = new List<double>();  //  Promedio puntaje
        public static List<double> registroGraf2 = new List<double>();  //  Mejor puntaje

        public int mejorFitness = 0;

        public Thread[] Hilo = new Thread[indexHilos];
        public Agente[] agentes = new Agente[IndexAgentes];

        public Agente mejorPadre = null;
        public Agente peorHijo1 = null;
        public Agente peorHijo2 = null;


        public Grafics grafico;


        public Form1()
        {
            InitializeComponent();
            label8.Text = delta.ToString();
            Random rnd = new Random();
            CheckForIllegalCrossThreadCalls = false;

            timer1.Interval = delta;
        }
        
        
        private void timer1_Tick(object sender, EventArgs e)
        {

            if (GameMode == 1)
            {
                if(c == 0)
                {
                    if (primeraIteracion)
                    {
                        agentes[0] = new Agente();
                        agentes[0].cabeza = new Cola(10, 10);
                        agentes[0].comida = new Comida();
                        reiniciar(agentes[0]);

                        primeraIteracion = false;
                    }
                    
                }
                ProcessG(agentes[0]);
            }
                

            else if(GameMode == 2)
            {

                if (primeraIteracion)
                {
                    for(int i = 0; i < IndexAgentes; i++)
                    {
                        
                        agentes[i] = new Agente();
                        agentes[i].cabeza = new Cola(10, 10);
                        agentes[i].comida = new Comida();
                        agentes[i].indice = i;
                        //agentes[i].resetAllNeurons(-3,3);
                        //Hilo[local] = new Thread(() => ProcessGame.ProcessGameThread(agentes[i]));
                        reiniciar(agentes[i], true);
                    }


                    mejorPadre = new Agente();
                    peorHijo1 = new Agente();
                    peorHijo2 = new Agente();
                    mejorPadre.fitness = 0;
                    peorHijo1.fitness = 2000;
                    peorHijo2.fitness = 2000;


                    label17.Text = "Cargando modelos...";
                    //agentes = Processess.LoadModelsFromXML(agentes, IndexAgentes, "C:/Users/Binary/Desktop/neuronsRandom.xml");

                    if(File.Exists("C:/Users/Binary/Desktop/BestModelGen.xml"))
                        agentes[0] = Processess.LoadModelFromXML(agentes[0], "C:/Users/Binary/Desktop/BestModelGen.xml");

                    label17.Text = "En ambiente...";

                    primeraIteracion = false;
                }

                


                //  Procesar Juego usando hilos por agente

                if(indexHilos > 1)
                {
                    if(indexHilos != IndexAgentes)
                    {
                        throw new System.IndexOutOfRangeException("No coincide el número de hilos con el número de agentes");
                    }


                    //  Verificar que todos los hilos hallan terminado el proceso de ejecución
                    try
                    {
                        int time = 0;
                        int tiempoespera = 20;
                        int dsfs = -1;
                        while (verificarHilosEjecucion(Hilo, agentes))
                        {
                            Thread.Sleep(tiempoespera);
                            time += tiempoespera;
                            if (time > 2000)
                            {
                                dsfs = verificarHilodespierto(Hilo);

                                if(dsfs != -1)
                                {
                                    agentes[dsfs].isAlive = false;
                                    Hilo[dsfs].Abort();
                                }
                            }
                        }
                    }
                    catch (NullReferenceException)
                    {

                    }

                    haySobrevivientes = false;

                    //  Verificar qué agentes quedan vivos (si quedan).

                    for (int i = 0; i < agentes.Length; i++)
                    {

                        IagentesVivos[i] = true;

                        if (!agentes[i].isAlive)
                        {
                            IagentesVivos[i] = false;
                        }
                        else
                        {
                            haySobrevivientes = true;
                            continue;
                        }
                    }

                    //agentesvivos = 2000;

                    if (haySobrevivientes)
                    {
                        for (int i = 0; i < indexHilos; i++)
                        {
                            if (IagentesVivos[i])
                            {
                                int local = i;
                                try
                                {
                                    Hilo[local] = new Thread(() => Processess.ProcessGameThread(agentes[local]));
                                    Hilo[local].Start();
                                }
                                catch (IndexOutOfRangeException)
                                {
                                    Hilo[local].Abort();
                                    agentes[local].isAlive = false;
                                }
                                
                            }
                            else
                            {
                            }
                        }
                    }
                    else
                    {
                        //MessageBox.Show("Nueva generación");

                        label17.Text = "En cruza";

                        for (int i = 0; i < IndexAgentes; i++)
                        {
                            agentes[i].isAlive = true;
                        }


                        agentes = algoritmoGenetico(agentes);

                        if(generacion%10 == 0 && guardarmodelos)
                            Processess.SaveModelsFile(agentes, @"c:\Users\Binary\Desktop\Modelsgen" + generacion.ToString() + ".dat");


                        registroGraf1.Add(promedioPuntaje);
                        registroGraf2.Add(mejorFitness);
                        //registroGraf2.Add(mejorPadre.fitness);


                        if (generacion % 2 == 0)
                            escribirDatos();

                        if(generacion == 100)
                        {
                            timer1.Stop();
                            label17.Text = "ppp";
                            MessageBox.Show("Se ha finalizado el algoritmo genético.\nCargados los mejores modelos de las últimas 3 generaciones.");
                            return;
                        }

                        generacion++;

                        label14.Text = generacion.ToString();
                        label17.Text = "En ambiente...";

                        haySobrevivientes = true;


                    }
                }

                

                //  Código que se ejecutará en caso de que se haga todo el proceso de modo secuencial y no paralelo, cambiando el Número de hilos a 1
                if(indexHilos == 0 || indexHilos == 1)
                {
                    for (int h = 0; h < IndexAgentes; h++)
                    {

                        if (agentes[h].isAlive)
                        {
                            haySobrevivientes = true;
                            agenteEjecutandose = h + 1;
                            Processess.ProcessGameThread(agentes[h]);

                            if (haySobrevivientes)
                                break;
                            else
                            {
                                if (h + 1 < IndexAgentes)
                                    if (agentes[h + 1].isAlive)
                                    {
                                        agentes[h + 1].cabeza = new Cola(10, 10);
                                        agentes[h + 1].comida = new Comida();
                                        agentes[h + 1].cabeza.meter();
                                        reiniciar(agentes[h + 1], true);
                                        haySobrevivientes = true;
                                    }

                                continue;
                            }
                        }
                    }

                    //  Detectar si la generación ya ha sido exterminada para iterar el algoritmo genético
                    if (!haySobrevivientes)
                    {
                        MessageBox.Show("Murieron todos");
                        for (int i = 0; i < IndexAgentes; i++)
                        {
                            agentes[i].isAlive = true;
                        }

                        algoritmoGenetico(agentes);

                        haySobrevivientes = true;

                    }
                }
                
                

            }

            else
            {
                if (primeraIteracion)
                {
                    label17.Text = "Cargando modelo...";

                    agentes[0] = new Agente();
                    agentes[0].cabeza = new Cola(10, 10);
                    agentes[0].comida = new Comida();
                    reiniciar(agentes[0]);

                    if (File.Exists("C:/Users/Binary/Desktop/BestModelGen.xml"))
                        agentes[0] = Processess.LoadModelFromXML(agentes[0], "C:/Users/Binary/Desktop/BestModelGen.xml");
                    else
                    {
                        timer1.Stop();
                        MessageBox.Show("No se ha detectado modelo de agente en la ruta seleccionada.");
                        GameMode = 1;
                        timer1.Start();
                    }
                    label10.Text = (Math.Abs(agentes[0].contP - agentes[0].limitepasosAI)).ToString();
                    label17.Text = "En ambiente...";

                    primeraIteracion = false;
                }
                else
                {
                    ProcessGameShowAgent(agentes[0]);

                }
            }
        }


        //  Función de  sobrecarga de process game que recibirá un agente por parámetro
        public void ProcessG(Agente agente)
        {
            if (!pausa)
            {

                //  Establecer matriz por defecto antes de refrescar datos

                for (int i = 0; i < 77; i++)
                {
                    for (int j = 0; j < 33; j++)
                    {
                        agente.matrixScreen[i, j] = 1;
                    }
                }

                //

                Graphics g = screen.CreateGraphics();




                agente.matrixScreen[agente.cabeza.verX() / 10, agente.cabeza.verY() / 10] = 2;
                agente.matrixScreen[agente.comida.x / 10, agente.comida.y / 10] = 0;


                



                this.KeyPreview = true;

                if (!debugmode) g.Clear(Color.Black);

                agente.matrixScreen = agente.cabeza.dibujar(g, agente.matrixScreen);

                movimiento(agente);



                //  Definicion de vector de entrada para las neuronas

                string vectorEntradastr = "";
                string vectoSalidastr = "";

                int[] vectorEntrada = new int[Agente.Inputs];
                int[] vectorSalida = new int[Agente.N_neuronas3];
                int[] vectemp;

                vectemp = Processess.calcularVectorEntradas2(agente, 10, 0);
                vectorEntrada[0] = vectemp[0];
                vectorEntrada[1] = vectemp[1];
                vectorEntrada[2] = vectemp[2];

                vectemp = Processess.calcularVectorEntradas2(agente, 10, 10);
                vectorEntrada[3] = vectemp[0];
                vectorEntrada[4] = vectemp[1];
                vectorEntrada[5] = vectemp[2];

                vectemp = Processess.calcularVectorEntradas2(agente, 10, -10);
                vectorEntrada[6] = vectemp[0];
                vectorEntrada[7] = vectemp[1];
                vectorEntrada[8] = vectemp[2];

                vectemp = Processess.calcularVectorEntradas2(agente, 0, -10);
                vectorEntrada[9] = vectemp[0];
                vectorEntrada[10] = vectemp[1];
                vectorEntrada[11] = vectemp[2];

                vectemp = Processess.calcularVectorEntradas2(agente, 0, 10);
                vectorEntrada[12] = vectemp[0];
                vectorEntrada[13] = vectemp[1];
                vectorEntrada[14] = vectemp[2];

                vectemp = Processess.calcularVectorEntradas2(agente, -10, 10);
                vectorEntrada[15] = vectemp[0];
                vectorEntrada[16] = vectemp[1];
                vectorEntrada[17] = vectemp[2];

                vectemp = Processess.calcularVectorEntradas2(agente, -10, 0);
                vectorEntrada[18] = vectemp[0];
                vectorEntrada[19] = vectemp[1];
                vectorEntrada[20] = vectemp[2];

                vectemp = Processess.calcularVectorEntradas2(agente, -10, -10);
                vectorEntrada[21] = vectemp[0];
                vectorEntrada[22] = vectemp[1];
                vectorEntrada[23] = vectemp[2];

                /*
                vectemp = Processess.calcularVectorEntradas(agente, 10, 0);
                vectorEntrada[0] = vectemp[0];
                vectorEntrada[1] = vectemp[1];
                vectorEntrada[2] = vectemp[2];
                vectorEntrada[3] = vectemp[3];

                vectemp = Processess.calcularVectorEntradas(agente, 10, 10);
                vectorEntrada[4] = vectemp[0];
                vectorEntrada[5] = vectemp[1];
                vectorEntrada[6] = vectemp[2];
                vectorEntrada[7] = vectemp[3];

                vectemp = Processess.calcularVectorEntradas(agente, 10, -10);
                vectorEntrada[8] = vectemp[0];
                vectorEntrada[9] = vectemp[1];
                vectorEntrada[10] = vectemp[2];
                vectorEntrada[11] = vectemp[3];

                vectemp = Processess.calcularVectorEntradas(agente, 0, -10);
                vectorEntrada[12] = vectemp[0];
                vectorEntrada[13] = vectemp[1];
                vectorEntrada[14] = vectemp[2];
                vectorEntrada[15] = vectemp[3];

                vectemp = Processess.calcularVectorEntradas(agente, 0, 10);
                vectorEntrada[16] = vectemp[0];
                vectorEntrada[17] = vectemp[1];
                vectorEntrada[18] = vectemp[2];
                vectorEntrada[19] = vectemp[3];

                vectemp = Processess.calcularVectorEntradas(agente, -10, 10);
                vectorEntrada[20] = vectemp[0];
                vectorEntrada[21] = vectemp[1];
                vectorEntrada[22] = vectemp[2];
                vectorEntrada[23] = vectemp[3];

                vectemp = Processess.calcularVectorEntradas(agente, -10, 0);
                vectorEntrada[24] = vectemp[0];
                vectorEntrada[25] = vectemp[1];
                vectorEntrada[26] = vectemp[2];
                vectorEntrada[27] = vectemp[3];

                vectemp = Processess.calcularVectorEntradas(agente, -10, -10);
                vectorEntrada[28] = vectemp[0];
                vectorEntrada[29] = vectemp[1];
                vectorEntrada[30] = vectemp[2];
                vectorEntrada[31] = vectemp[3];
                */

                //  Se le pasa el vector por parámetro a la red neuronal para que el agente decida su próximo movimiento

                vectemp = null;

                vectorSalida = agente.procesar(agente.ConvertToDouble(vectorEntrada));


                vectoSalidastr = $"{vectorSalida[0]}, {vectorSalida[1]}, {vectorSalida[2]}, {vectorSalida[3]}, ";

                for(int i = 0; i < vectorEntrada.Length; i++)
                {
                    vectorEntradastr += $"{vectorEntrada[i]}, ";
                }





                label18.Text = $"Entradas: {vectorEntradastr}";
                label17.Text = $"Salidas: {vectoSalidastr}";

                if(GameMode == 2)
                {
                    if (agente.cabeza.IsInXcoord)
                    {
                        if (vectorSalida[0] == 1)
                        {
                            agente.cabeza.Ydir = cuadro * -1;
                            agente.cabeza.Xdir = 0;
                            agente.cabeza.IsInXcoord = false;
                            agente.cabeza.IsInYcoord = true;
                        }
                        else if (vectorSalida[1] == 1)
                        {
                            agente.cabeza.Ydir = cuadro;
                            agente.cabeza.Xdir = 0;
                            agente.cabeza.IsInXcoord = false;
                            agente.cabeza.IsInYcoord = true;
                        }
                    }

                    else if (agente.cabeza.IsInYcoord)
                    {
                        if (vectorSalida[2] == 1)
                        {
                            agente.cabeza.Xdir = cuadro * -1;
                            agente.cabeza.Ydir = 0;
                            agente.cabeza.IsInYcoord = false;
                            agente.cabeza.IsInXcoord = true;
                        }
                        else if (vectorSalida[3] == 1)
                        {
                            agente.cabeza.Xdir = cuadro;
                            agente.cabeza.Ydir = 0;
                            agente.cabeza.IsInYcoord = false;
                            agente.cabeza.IsInXcoord = true;
                        }
                    }
                }



                if (debugmode) vistaIAMatrix(g, agente.matrixScreen);

                //  Pintar comida
                if (!debugmode) g.FillRectangle(new SolidBrush(Color.Red), agente.comida.x, agente.comida.y, cuadro, cuadro);


                if (agente.cabeza.addfood(agente.comida))
                {
                    agente.comida.establecerComida();
                    agente.Puntaje += 10;
                    agente.cabeza.meter();
                    //label2.Text = mejorPadre.fitness.ToString();

                    agente.matrixScreen[agente.comida.x / 10, agente.comida.y / 10] = 0;

                    //  temporalmente 
                    if (Math.Abs(agente.contP - agente.limitepasosAI) < 1800)
                        agente.limitepasosAI += 200;

                }

                if (agente.cabeza.Collision() || colisioncuerpo(agente))
                {
                    if (GameMode == 1)
                    {
                        timer1.Stop();
                        //MessageBox.Show("Perdiste.");
                        timer1.Start();
                    }
                    agente.isAlive = false;
                    reiniciar(agente);
                    return;
                }

                //  temporalmente se llamara siempre esta funcion
                IAProcess(agente);

                if (GameMode == 1)
                {
                    //label4.Text = "";
                    //label5.Text = "";
                    //label9.Text = "";
                    //label10.Text = "";
                    //label5.Text = mejorPadre.fitness.ToString();

                    label9.Text = "Movimientos restantes:";
                    label10.Text = Math.Abs(agente.contP - agente.limitepasosAI).ToString();
                    label12.Text = agenteEjecutandose.ToString();
                }

                if (GameMode == 2)
                {
                    label5.Text = agente.fitness.ToString();

                    label10.Text = Math.Abs(agente.contP - agente.limitepasosAI).ToString();
                    label12.Text = agenteEjecutandose.ToString();
                }
            }
        }

        public void ProcessGameShowAgent(Agente agente)
        {

            //  Verificar que el agente esté con vida
            if (!agente.isAlive)
                return;

            //  Refrescar la matriz del ambiente del agente

            for (int i = 0; i < 77; i++)
            {
                for (int j = 0; j < 33; j++)
                {
                    agente.matrixScreen[i, j] = 1;
                }
            }

            Graphics g = screen.CreateGraphics();


            //  Registrar en el ambiente la ubicación de la cabeza, comida y cola

            //int cabezax = checked(Math.Abs(agente.cabeza.x / 10));
            //int cabezay = checked(Math.Abs(agente.cabeza.y / 10));

            //int comidax = checked(Math.Abs(agente.comida.x / 10));
            //int comiday = checked(Math.Abs(agente.comida.y / 10));

            int cabezax = agente.cabeza.x / 10;
            int cabezay = agente.cabeza.y / 10;

            int comidax = agente.comida.x / 10;
            int comiday = agente.comida.y / 10;

            if (cabezax >= 77 || cabezay >= 33)
            {
                agente.isAlive = false;
                return;
            }


            agente.matrixScreen[cabezax, cabezay] = 2;
            agente.matrixScreen[comidax, comiday] = 0;


            g.Clear(Color.Black);
            agente.matrixScreen = agente.cabeza.dibujar(g, agente.matrixScreen);


            Form1.movimiento(agente);

            int[] vectorEntrada = new int[Agente.Inputs];
            int[] vectorSalida = new int[Agente.N_neuronas3];
            int[] vectemp;

            vectemp = Processess.calcularVectorEntradas2(agente, 10, 0);
            vectorEntrada[0] = vectemp[0];
            vectorEntrada[1] = vectemp[1];
            vectorEntrada[2] = vectemp[2];

            vectemp = Processess.calcularVectorEntradas2(agente, 10, 10);
            vectorEntrada[3] = vectemp[0];
            vectorEntrada[4] = vectemp[1];
            vectorEntrada[5] = vectemp[2];

            vectemp = Processess.calcularVectorEntradas2(agente, 10, -10);
            vectorEntrada[6] = vectemp[0];
            vectorEntrada[7] = vectemp[1];
            vectorEntrada[8] = vectemp[2];

            vectemp = Processess.calcularVectorEntradas2(agente, 0, -10);
            vectorEntrada[9] = vectemp[0];
            vectorEntrada[10] = vectemp[1];
            vectorEntrada[11] = vectemp[2];

            vectemp = Processess.calcularVectorEntradas2(agente, 0, 10);
            vectorEntrada[12] = vectemp[0];
            vectorEntrada[13] = vectemp[1];
            vectorEntrada[14] = vectemp[2];

            vectemp = Processess.calcularVectorEntradas2(agente, -10, 10);
            vectorEntrada[15] = vectemp[0];
            vectorEntrada[16] = vectemp[1];
            vectorEntrada[17] = vectemp[2];

            vectemp = Processess.calcularVectorEntradas2(agente, -10, 0);
            vectorEntrada[18] = vectemp[0];
            vectorEntrada[19] = vectemp[1];
            vectorEntrada[20] = vectemp[2];

            vectemp = Processess.calcularVectorEntradas2(agente, -10, -10);
            vectorEntrada[21] = vectemp[0];
            vectorEntrada[22] = vectemp[1];
            vectorEntrada[23] = vectemp[2];

            //  Se le pasa el vector por parámetro a la red neuronal para que el agente decida su próximo movimiento

            vectorSalida = agente.procesar(agente.ConvertToDouble(vectorEntrada));


            if (agente.cabeza.IsInXcoord)
            {
                if (vectorSalida[0] == 1)
                {
                    agente.cabeza.Ydir = Form1.cuadro * -1;
                    agente.cabeza.Xdir = 0;
                    agente.cabeza.IsInXcoord = false;
                    agente.cabeza.IsInYcoord = true;
                }
                else if (vectorSalida[1] == 1)
                {
                    agente.cabeza.Ydir = Form1.cuadro;
                    agente.cabeza.Xdir = 0;
                    agente.cabeza.IsInXcoord = false;
                    agente.cabeza.IsInYcoord = true;
                }
            }

            else if (agente.cabeza.IsInYcoord)
            {
                if (vectorSalida[2] == 1)
                {
                    agente.cabeza.Xdir = Form1.cuadro * -1;
                    agente.cabeza.Ydir = 0;
                    agente.cabeza.IsInYcoord = false;
                    agente.cabeza.IsInXcoord = true;
                }
                else if (vectorSalida[3] == 1)
                {
                    agente.cabeza.Xdir = Form1.cuadro;
                    agente.cabeza.Ydir = 0;
                    agente.cabeza.IsInYcoord = false;
                    agente.cabeza.IsInXcoord = true;
                }
            }

            g.FillRectangle(new SolidBrush(Color.Red), agente.comida.x, agente.comida.y, cuadro, cuadro);

            //  Detectar colisión con comida

            if (agente.cabeza.addfood(agente.comida))
            {
                agente.comida.establecerComida();
                agente.cabeza.meter();
                agente.matrixScreen[agente.comida.x / 10, agente.comida.y / 10] = 0;

                if (Math.Abs(agente.contP - agente.limitepasosAI) < 1800)
                    agente.limitepasosAI += 200;
            }

            //  Detectar Colisión con cuerpo o paredes
            if (agente.cabeza.Collision() || Form1.colisioncuerpo(agente))
            {
                agente.isAlive = false;
                reiniciar(agente);
                return;
            }

            Form1.IAProcess(agente);

        }

        //  Esta función solo se utiliza para pruebas o debug, para ver el comportamiento de la IA en función a lo que ella percibe
        public void vistaIAMatrix(Graphics g, int[,] matrixScreen)
        {
            //Thread t1;
            //Thread t2;
            //Thread t3;

            //  Hilos que pinten la información dentro de la caja de imágen
            //t1 = new Thread(new ThreadStart(() => vistaIAMatrixThread1(g)));
            //t2 = new Thread(new ThreadStart(() => vistaIAMatrixThread2(g)));
            //t3 = new Thread(new ThreadStart(() => vistaIAMatrixThread3(g)));

            //if (t1.IsAlive) ;


            for (int i = 0; i < 77; i++)
            {
                for (int j = 0; j < 33; j++)
                {
                    if (matrixScreen[i, j] == 1)
                        g.FillRectangle(new SolidBrush(Color.Blue), i * 10, j * 10, cuadro, cuadro);
                    else if (matrixScreen[i, j] == 2)
                        g.FillRectangle(new SolidBrush(Color.Black), i * 10, j * 10, cuadro, cuadro);
                    else if (matrixScreen[i, j] == 0)
                        g.FillRectangle(new SolidBrush(Color.Yellow), i * 10, j * 10, cuadro, cuadro);
                    else if(matrixScreen[i, j] == 8)
                        g.FillRectangle(new SolidBrush(Color.Yellow), i * 10, j * 10, cuadro, cuadro);
                    else if(matrixScreen[i, j] == 7)
                        g.FillRectangle(new SolidBrush(Color.Orange), i * 10, j * 10, cuadro, cuadro);
                    else if (matrixScreen[i, j] == 9)
                        g.FillRectangle(new SolidBrush(Color.Chocolate), i * 10, j * 10, cuadro, cuadro);
                }
            }
        }


        //  Procesos que se llevarán a cabo en el modo de juego 2 (IA)
        public static void IAProcess(Agente agente)
        {
            agente.contP++;
            agente.fitness = (agente.contP * (agente.Puntaje / 2 + 1));

            if(agente.contP >= agente.limitepasosAI)
            {
                agente.isAlive = false;
                reiniciar(agente, true);
            }

        }


        public static bool colisioncuerpo(Agente agente)
        {
            Cola temp;

            try
            {
                temp = agente.cabeza.versiguiente().versiguiente();
            }
            catch (Exception)
            {
                temp = null;
            }

            while( temp != null)
            {
                //  Se aprovecha el bucle para actualizar las coordenadas del cuerpo en matriz
                agente.matrixScreen[temp.verX()/10, temp.verY()/10] = 2;
                agente.matrixScreen[agente.cabeza.verX()/10, agente.cabeza.verY()/10] = 2;

                //  Se comprueba que no hayan colisiones de cabeza - cuerpo
                if (agente.cabeza.verX() == temp.verX() && agente.cabeza.verY() == temp.verY())
                    return true;
                else
                    temp = temp.versiguiente();
            }

            return false;
        }

        //  Actualiza las coordenadas en el eje X y en el eje Y en la matríz
        public static void movimiento(Agente agente)
        {
            agente.cabeza.setxy(agente.cabeza.verX() + agente.cabeza.Xdir, agente.cabeza.verY() + agente.cabeza.Ydir);
        }


        //  Sobrecarga en caso de que el gamemode sea = 2, recibirá el agente por parámetro de entrada
        public void reiniciar(Agente agente)
        {

            Random rnd = new Random();
            agente.cabeza = new Cola(rnd.Next(10, 60)*10, rnd.Next(10, 30)*10);
            agente.comida = new Comida();
            agente.contP = 0;
            agente.Puntaje = 0;
            agente.fitnessAnt = agente.fitness;
            agente.limitepasosAI = 500;
            agente.comida.establecerComida();
            agente.cabeza.meter();
            agente.cabeza.meter();
            label2.Text = agente.Puntaje.ToString();
            label14.Text = generacion.ToString();
            label16.Text = IndexAgentes.ToString();
            haySobrevivientes = false;

        }

        //  Sobrecarga del método para llamada desde un hilo
        public static void reiniciar(Agente agente, bool thread)
        {

            Random rnd = new Random();
            agente.cabeza = new Cola(rnd.Next(10, 60) * 10, rnd.Next(10, 30) * 10);
            agente.comida = new Comida();
            agente.contP = 0;
            agente.Puntaje = 0;
            agente.fitnessAnt = agente.fitness;
            agente.limitepasosAI = 500;
            agente.comida.establecerComida();
            agente.cabeza.meter();
            agente.cabeza.meter();
            haySobrevivientes = false;
            //puntaje = 0;
            //label2.Text = puntaje.ToString();
            //label14.Text = generacion.ToString();
            //label16.Text = IndexAgentes.ToString();

        }

        //  Funciones de tecla y cambios de dirección
        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (GameMode == 1)
            {
                if (agentes[0].cabeza.IsInXcoord)
                {
                    if (e.KeyCode == Keys.W)
                    {
                        agentes[0].cabeza.Ydir = cuadro * -1;
                        agentes[0].cabeza.Xdir = 0;
                        agentes[0].cabeza.IsInXcoord = false;
                        agentes[0].cabeza.IsInYcoord = true;
                    }
                    else if (e.KeyCode == Keys.S)
                    {
                        agentes[0].cabeza.Ydir = cuadro;
                        agentes[0].cabeza.Xdir = 0;
                        agentes[0].cabeza.IsInXcoord = false;
                        agentes[0].cabeza.IsInYcoord = true;
                    }
                }

                else if (agentes[0].cabeza.IsInYcoord)
                {
                    if (e.KeyCode == Keys.A)
                    {
                        agentes[0].cabeza.Xdir = cuadro * -1;
                        agentes[0].cabeza.Ydir = 0;
                        agentes[0].cabeza.IsInYcoord = false;
                        agentes[0].cabeza.IsInXcoord = true;
                    }
                    else if (e.KeyCode == Keys.D)
                    {
                        agentes[0].cabeza.Xdir = cuadro;
                        agentes[0].cabeza.Ydir = 0;
                        agentes[0].cabeza.IsInYcoord = false;
                        agentes[0].cabeza.IsInXcoord = true;
                    }
                }
            }


            if (e.KeyCode == Keys.Multiply)
            {
                if (delta > 1)
                    delta = delta / 2;
                label8.Text = delta.ToString();
                timer1.Interval = delta;
            }

            else if (e.KeyCode == Keys.Divide)
            {
                if (delta < 1000)
                    delta = delta * 2;
                label8.Text = delta.ToString();
                timer1.Interval = delta;
            }

            else if (e.KeyCode == Keys.P)
            {
                if (timer1.Enabled)
                {
                    timer1.Stop();
                }
                else
                {
                    timer1.Start();
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

            if (pausa == true)
            {
                pausa = false;
                button1.Text = "Detener";
            }
            else
            {
                button1.Text = "Reiniciar";
                pausa = true;
            }

            primeraIteracion = true;

        }


        //************* Activar o Desactivar Jugador / IA **************
        private void button2_Click(object sender, EventArgs e)
        {
            if (!pausa)
                button1.PerformClick();
            button1.Text = "Iniciar";


            if (GameMode == 1)
                GameMode = 2;
            else GameMode = 1;


            if (GameMode == 2)
            {
                button2.Text = "Modo Inteligencia Artificial Activado";
                label6.Text = "Entrenamiento IA.";
            }


            else if (GameMode == 1)
            {
                button2.Text = "Modo Jugador Activado";
                label6.Text = "Jugador.";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (timer1.Enabled)
            {
                if (button3.Text == "Deshabilitar actividad de agentes")
                {
                    button3.Text = "Continuar con la ejecución del algoritmo genético";
                }
                else
                {
                    button3.Text = "Deshabilitar actividad de agentes";
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (button4.Text == "Pausar")
            {
                button4.Text = "Reanudar";
            }
            else
            {
                button4.Text = "Pausar";
            }
            if (timer1.Enabled)
            {
                timer1.Stop();
            }
            else
            {
                timer1.Start();
            }
        }


        private void button5_Click(object sender, EventArgs e)
        {
            if (debugmode)
                debugmode = false;
            else debugmode = true;
        }




        public static bool verificarHilosEjecucion(Thread[] hilos, Agente[] agentes)
        {
            bool ejecutando = false;

            for(int i = 0; i < hilos.Length; i++)
            {
                if (hilos[i].IsAlive)
                {
                    if (!agentes[i].isAlive)
                        hilos[i].Abort();

                    else
                        return true;
                }
            }

            return ejecutando;
        }

        public static int verificarHilodespierto(Thread[] hilos)
        {
            for (int i = 0; i < hilos.Length; i++)
            {
                if (hilos[i].IsAlive)
                {
                    return i;
                }
            }

            return -1;
        }



        public void escribirDatos()
        {
            
            using (StreamWriter sw = File.CreateText(estadisticasPath))
            {

                sw.WriteLine("Generación\tPromedio Fitness\tMejor Fitness");

                for (int i = 0; i < registroGraf1.Count; i++)
                {
                    sw.WriteLine($"{i+1}\t{registroGraf1[i]}\t{registroGraf2[i]}");
                    //if(registroGraf2[i] == 1000 || registroGraf2[i] == 0)
                    //{
                    //    MessageBox.Show("");
                    //}
                }

                //sw.WriteLine($"{generacion}\t{promedioPuntaje}\t{mejorPadre.fitness}\n");

                sw.Flush();
                sw.Close();
                sw.Dispose();
            }
        }


        
        public void buscarMejorFitness(Agente[] agente)
        {
            mejorFitness = 0;
            indiceMejorPadre = -1;

            for(int i = 0; i < agente.Length; i++)
            {
                if (agente[i].fitness > mejorFitness)
                {
                    //mejorPadre = agente[i];
                    mejorFitness = agente[i].fitness;
                    indiceMejorPadre = i;
                }
            }
        }

        public Agente[] CruceGenetico(Agente[] padres)
        {
            Random rnd = new Random();
            Agente[] hijos = padres;

            for (int i = 0; i < hijos.Length; i += 2)
            {

                int temprand1 = rnd.Next(1, padres[i].genotipo_1.GetLength(0) - 1);
                int temprand2 = rnd.Next(1, padres[i].genotipo_2.GetLength(0) - 1);
                int temprand3 = rnd.Next(1, padres[i].genotipo_3.GetLength(0) - 1);


                for (int j = temprand1; j < hijos[i].genotipo_1.GetLength(0); j++)
                {
                    for (int k = 0; k < hijos[i].genotipo_1.GetLength(1); k++)
                    {
                        hijos[i].genotipo_1[j, k] = padres[i + 1].genotipo_1[j, k];
                    }
                }

                for (int j = temprand2; j < hijos[i].genotipo_2.GetLength(0); j++)
                {
                    for (int k = 0; k < hijos[i].genotipo_2.GetLength(1); k++)
                    {
                        hijos[i].genotipo_2[j, k] = padres[i + 1].genotipo_2[j, k];
                    }
                }

                for (int j = temprand3; j < hijos[i].genotipo_3.GetLength(0); j++)
                {
                    for (int k = 0; k < hijos[i].genotipo_3.GetLength(1); k++)
                    {
                        hijos[i].genotipo_3[j, k] = padres[i + 1].genotipo_3[j, k]; 
                    }
                }

                //

                for (int j = 0; j < temprand1; j++)
                {
                    for (int k = 0; k < hijos[i + 1].genotipo_1.GetLength(1); k++)
                    {
                        hijos[i + 1].genotipo_1[j, k] = padres[i + 1].genotipo_1[j, k];
                    }
                }

                for (int j = 0; j < temprand2; j++)
                {
                    for (int k = 0; k < hijos[i + 1].genotipo_2.GetLength(1); k++)
                    {
                        hijos[i + 1].genotipo_2[j, k] = padres[i + 1].genotipo_2[j, k];
                    }
                }

                for (int j = 0; j < temprand3; j++)
                {
                    for (int k = 0; k < hijos[i + 1].genotipo_3.GetLength(1); k++)
                    {
                        hijos[i + 1].genotipo_3[j, k] = padres[i + 1].genotipo_3[j, k];
                    }
                }
            }

            return hijos;
        }

        public void SetMejorPadre(Agente[] agente)
        {
            mejorPadre.fitnessAnt = 0;
            indiceMejorPadre = -1;
            for (int i = 0; i < agente.Length; i++)
            {
                if (agente[i].fitnessAnt > mejorPadre.fitnessAnt)
                {
                    mejorPadre = agente[i];
                    indiceMejorPadre = i;
                }
                if(agente[i].fitnessAnt >= 100)
                {

                }
            }
        }

        public void SetPeorHijo(Agente[] agente)
        {
            peorHijo1.fitness = 2000;
            peorHijo2.fitness = 2000;
            indicePeorHijo1 = -1;
            indicePeorHijo2 = -1;

            for (int i = 0; i < agente.Length; i++)
            {
                if (agente[i].fitnessAnt < peorHijo1.fitness)
                {
                    peorHijo1 = agente[i];
                    indicePeorHijo1 = i;
                }
                else if(agente[i].fitnessAnt < peorHijo2.fitness)
                {
                    peorHijo2 = agente[i];
                    indicePeorHijo2 = i;
                }
            }
        }


        public Agente[] seleccionPadres(Agente[] agente)
        {
            int sumFitness = 0;

            int[] ruleta = new int[1000];

            Agente[] padres = new Agente[agente.Length];

            //mejorPadre.fitness = 0;
            //mejorFitness = 0;

            for (int i = 0; i < agente.Length; i++)
            {
                sumFitness += int.Parse((agente[i].fitness * 100).ToString());

            }

            //  Se aprovecha la variable sumFitness para calcular el promedio de los fitness
            promedioPuntaje = (sumFitness / 100) / agente.Length;

            //Processess.SaveModelFileXML(mejorPadre, @"C:/Users/Binary/Desktop/BestModelGen" + generacion.ToString() + ".xml");

            //	Inicializar Ruleta de selección
            for (int j = 0; j < ruleta.Length; j++)
            {
                ruleta[j] = -1;
            }

            //	Establecer valores en la ruleta
            for (int i = 0; i < agente.Length; i++)
            {
                if(generacion >= 2)
                {

                }
                double var1 = 0;
                int porcentemp = 0;
                int k = 1;
                var1 = ((agente[i].fitness * 100.0) / sumFitness) * 1000.0;
                porcentemp = int.Parse(Math.Round(var1).ToString());

                if(porcentemp == 0)
                {
                    for(int j = 0; j < ruleta.Length; j++)
                    {
                        if (ruleta[j] == -1)
                        {
                            Random random = new Random();
                            ruleta[j] = random.Next(0, agente.Length - 1);
                            break;
                        }
                    }
                    continue;
                }

                for (int j = 0; j < ruleta.Length; j++)
                {
                    if (ruleta[j] == -1)
                    {
                        if (k <= porcentemp)
                        {
                            ruleta[j] = i;
                            k++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }

            //	Empezar la iteración de selección de los mejores agentes en función a su aptitud
            for (int c = 0; c < padres.Length; c++)
            {
                int puntero = 0;
                Random random = new Random();
                int numero = random.Next(1, ruleta.Length * 3);
                //	Iterar sobre la ruleta para seleccionar los próximos padres
                for (int i = 1; i <= numero; i++)
                {
                    if (puntero == ruleta.Length - 1)
                    {
                        puntero = 0;
                    }
                    else puntero++;
                }

                padres[c] = agente[ruleta[puntero]];
            }

            return padres;
        }


        public Agente[] mutacion(Agente[] hijos)
        {
            Random rnd = new Random();

            for(int i = 0; i < hijos.Length; i++)
            {
                for(int j = 0; j < hijos[i].genotipo_1.GetLength(0); j++)
                {
                    for(int k = 0; k < hijos[i].genotipo_1.GetLength(1); k++)
                    {
                        if(rnd.Next(1, 10) == 4)     //  Probabilidad de 20 % de aplicar una mutación del 10 % - 20 %
                        {
                            if (rnd.Next(0, 2) == 0)
                                hijos[i].genotipo_1[j, k] += hijos[i].genotipo_1[j, k] * (rnd.Next(20, 70) / 100);
                            else
                                hijos[i].genotipo_1[j, k] += hijos[i].genotipo_1[j, k] * (rnd.Next(-70, -20) / 100);
                        }
                    }
                }

                for (int j = 0; j < hijos[i].genotipo_2.GetLength(0); j++)
                {
                    for (int k = 0; k < hijos[i].genotipo_2.GetLength(1); k++)
                    {
                        if (rnd.Next(1, 10) == 4)     //  Probabilidad de 20 % de aplicar una mutación del 10 % - 20 %
                        {
                            if (rnd.Next(0, 2) == 0)
                                hijos[i].genotipo_2[j, k] += hijos[i].genotipo_2[j, k] * (rnd.Next(20, 70) / 100);
                            else
                                hijos[i].genotipo_2[j, k] += hijos[i].genotipo_2[j, k] * (rnd.Next(-70, -20) / 100);
                        }
                    }
                }

                for (int j = 0; j < hijos[i].genotipo_3.GetLength(0); j++)
                {
                    for (int k = 0; k < hijos[i].genotipo_3.GetLength(1); k++)
                    {
                        if (rnd.Next(1, 10) == 4)     //  Probabilidad de 20 % de aplicar una mutación del 10 % - 20 %
                        {
                            if(rnd.Next(0,2) == 0)
                                hijos[i].genotipo_3[j, k] += hijos[i].genotipo_3[j, k] * (rnd.Next(20, 70) / 100);
                            else
                                hijos[i].genotipo_3[j, k] += hijos[i].genotipo_3[j, k] * (rnd.Next(-70, -20) / 100);
                        }
                    }
                }
            }
            
            return hijos;
        }

        public Agente[] elitismo(Agente[] agentes)
        {
            if (generacion == 1)
                return agentes;

            agentes[indicePeorHijo1] = mejorPadre;
            agentes[indicePeorHijo2] = mejorPadre;

            indiceMejorPadre = indicePeorHijo1;
            indicePeorHijo1 = -1;
            indicePeorHijo2 = -1;

            return agentes;
        }

        public Agente[] reiniciarFitness(Agente[] agente)
        {
            for(int i = 0; i < agente.Length; i++)
            {
                agente[i].fitness = 0;
            }

            return agente;
        }

        public Agente[] algoritmoGenetico(Agente[] agente)
        {

            Agente[] padres = new Agente[agente.Length];
            Agente[] hijos = new Agente[agente.Length];

            //SetMejorPadre(agente);

            if(generacion > 1)
            {
                //SetPeorHijo(agente);

                //agente = elitismo(agente);
            }

            padres = seleccionPadres(agente);


            for (int i = 0; i < padres.Length; i++)
            {
                padres[i].actualizarGenotipo();
            }

            hijos = CruceGenetico(padres);

            hijos = mutacion(hijos);

            buscarMejorFitness(agente);

            hijos = reiniciarFitness(hijos);

            for(int i = 0; i < hijos.Length; i++)
            {
                hijos[i].actualizarCromosoma();
            }

            return hijos;
        }
    }
}
