using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NeuralNetwork;

namespace WindowsFormsApp2
{

    public class Comida
    {
        Random rnd = new Random();

        public int x;
        public int y;

        public void establecerComida()
        {
            x = rnd.Next(1, 76) * 10;
            y = rnd.Next(1, 32) * 10;
        }

    }

    public class Cola
    {

        public int Xdir = 10;
        public int Ydir = 0;
        public bool IsInXcoord = false;
        public bool IsInYcoord = false;

        public int x, y;
        int ancho = 10;

        
        Cola siguiente = null;

        public Cola(int x, int y)
        {
            this.x = x;
            this.y = y;

            this.x = 10;
            this.y = 10;

            IsInXcoord = true;
        }

        //  Función que pinta y refresca coordenadas de cola y cabeza en la matriz
        public int[,] dibujar(Graphics g, int[,] matrix)
        {

            matrix[x / 10, y / 10] = 2;

            if (siguiente != null)
            {
                siguiente.dibujar(g, matrix);
            }

            if (!Form1.debugmode) g.FillRectangle(new SolidBrush(Color.White), x, y, ancho, ancho);

            return matrix;
        }

        //  Sobrecarga para el uso del agente en un hilo de subproceso que no requiere pintar
        public int[,] dibujar(int[,] matrix)
        {

            matrix[x / 10, y / 10] = 2;

            if (siguiente != null)
            {
                siguiente.dibujar(matrix);
            }

            return matrix;
        }

        public void setxy(int x, int y)
        {
            if(siguiente != null)
            {
                siguiente.setxy(this.x, this.y);
            }
            
            this.x = x;
            this.y = y;
        }

        public void meter()
        {
            if(siguiente == null)
            {
                siguiente = new Cola(this.x, this.y);
            }

            else
            {
                siguiente.meter();
            }
        }

        public int verX()
        {
            return x;
        }
        public int verY()
        {
            return y;
        }

        public bool addfood(Comida comida)
        {

            if (x == comida.x && y == comida.y)
            {
                return true;
            }
            else return false;
        }

        public bool Collision()
        {
            if (x < 0 || y < 0)
                return true;

            else if (x >= 770 || y >= 330)
                return true;
            return false;
            
        }

        public Cola versiguiente()
        {
            return siguiente;
        }
    }

    public class Agente
    {
        public Cola cabeza;
        public Comida comida;

        public Layer[] neuronas = new Layer[3];

        private Neuron[] neurona_1;
        private Neuron[] neurona_2;
        private Neuron[] neurona_3;
        

        //  Matriz[ x , y ] de juego para la visión de la IA
        public int[,] matrixScreen = new int[77, 33];

        public int contP = 0;
        public int limitepasosAI = 500;
        public int Puntaje = 0;

        public bool isAlive;

        public const int Inputs = 24;
        public const int FuncionActivacion = 1;
        public const int N_neuronas1 = 18;
        public const int N_neuronas2 = 18;
        public const int N_neuronas3 = 4;   //  Capa de salida WASD

        public int fitness = 0;
        public int fitnessAnt = 0;
        public int indice = 0;

        public double[,] genotipo_1;
        public double[,] genotipo_2;
        public double[,] genotipo_3;

        private Random rnd = new Random();

        public Agente()
        {
            contP = 0;
            limitepasosAI = 500;
            fitness = 0;

            neurona_1 = new Neuron[N_neuronas1];
            neurona_2 = new Neuron[N_neuronas2];
            neurona_3 = new Neuron[N_neuronas3];

            isAlive = true;

            //  Instanciamiento aleatorio de neuronas
            for (int i = 0; i < N_neuronas1; i++)
            {
                neurona_1[i] = new Neuron(Inputs, FuncionActivacion);
                //neurona_1[i].resetNeuron(rnd.Next(-3, 0), rnd.Next(0, 3));
            }
            for (int i = 0; i < N_neuronas2; i++)
            {
                neurona_2[i] = new Neuron(N_neuronas1, FuncionActivacion);
                //neurona_2[i].resetNeuron(rnd.Next(-3, 0), rnd.Next(0, 3));
            }
            for (int i = 0; i < N_neuronas3; i++)
            {
                neurona_3[i] = new Neuron(N_neuronas2, FuncionActivacion);
                //neurona_3[i].resetNeuron(rnd.Next(-3, 0), rnd.Next(0, 3));
            }


            neuronas[0] = new Layer(neurona_1);
            neuronas[1] = new Layer(neurona_2);
            neuronas[2] = new Layer(neurona_3);


            genotipo_1 = new double[N_neuronas1, Inputs + 1];       //  +1 Elemento polarización
            genotipo_2 = new double[N_neuronas2, N_neuronas1 + 1];  //  +1 Elemento polarización
            genotipo_3 = new double[N_neuronas3, N_neuronas2 + 1];  //  +1 Elemento polarización

            neurona_1 = null;
            neurona_2 = null;
            neurona_3 = null;


        }


        //  Sobrecarga en caso de que se cargue un modelo de genotipo ya apto para el ambiente de pruebas
        public Agente(Neuron[] neuronas1, Neuron[] neuronas2, Neuron[] neuronas3)
        {
            //pass
        }

        

        public void setCola(int x, int y)
        {
            cabeza = new Cola(x, y);
        }

        public int LengthInputs()
        {
            return Inputs;
        }

        public int LengthOutputs()
        {
            return N_neuronas3;
        }



        //  Método inicia el procesamiento de datos mediante las capas
        /*  
         *  El agente recibirá un vector de tipo double como parámetro de entrada para las neuronas, en las cuales evaluará en 7 direcciones
         *  (frente, derecha, izquierda y diagonales):
         *      - A qué distancia hay comida
         *      - A qué distancia están las paredes
         *      - A qué distancia está su propio cuerpo
         *      
         *      Dando un total de 21 neuronas que se representan en el vector de entrada
         *      se envía a la función procesar(), la cual enviará ese vector de entrada a la red neuronal, hará los calculos y
         *      devolverá un vector de 4 elementos que representa la neurona de salida que definirá su próximo movimiento en cada frame. (W A S D)
         * 
         */

        public int[] procesar(double[] entrada)
        {
            //return ConvertToInt(neuronas[2].getAllAxis(neuronas[1].getAllAxis(neuronas[0].getAllAxis(entrada))));

            return ConvertToInt(neuronas[2].getAllAxis(neuronas[1].getAllAxis(neuronas[0].getAllAxis(entrada))));
        }

        public double[] ConvertToDouble(int[] vector)
        {
            double[] vectorConverted = new double[vector.Length];

            for(int i = 0; i < vector.Length; i++)
            {
                vectorConverted[i] = vector[i];
            }

            return vectorConverted;
        }

        public int[] ConvertToInt(double[] vector)
        {
            int[] vectorConverted = new int[vector.Length];

            for (int i = 0; i < vector.Length; i++)
            {
                vectorConverted[i] = Convert.ToInt32(vector[i]);
            }

            return vectorConverted;
        }

        public void resetAllNeurons(int from, int to)
        {
            int var = neurona_1.Length;
            for (int i = 0; i < var; i++)
            {
                neurona_1[i].resetNeuron(rnd.Next(from, 0), rnd.Next(0, to));
            }

            var = neurona_2.Length;
            for (int i = 0; i < var; i++)
            {
                neurona_2[i].resetNeuron(rnd.Next(from, 0), rnd.Next(0, to));
            }

            var = neurona_3.Length;
            for (int i = 0; i < var; i++)
            {
                neurona_3[i].resetNeuron(rnd.Next(from, 0), rnd.Next(0, to));
            }
        }

        public void actualizarGenotipo()
        {
            //  Actualización de genotipo para capa 1 de la red
            for (int i = 0; i < N_neuronas1; i++)
            {
                for (int j = 0; j < Inputs + 1; j++)
                {
                    if (j != Inputs)
                        genotipo_1[i, j] = neuronas[0].N[i].getAW(j);   //neurona_1[i].getAW(j);

                    else
                        genotipo_1[i, Inputs] = neuronas[0].N[i].getB();


                }
            }
            //  Actualización de genotipo para capa 2 de la red
            for (int i = 0; i < N_neuronas2; i++)
            {
                for (int j = 0; j < N_neuronas1 + 1; j++)
                {
                    if (j != N_neuronas1)
                        genotipo_2[i, j] = neuronas[1].N[i].getAW(j);

                    else
                        genotipo_2[i, N_neuronas1] = neuronas[1].N[i].getB();

                }
            }
            //  Actualización de genotipo para capa 3 de la red
            for (int i = 0; i < N_neuronas3; i++)
            {
                for (int j = 0; j < N_neuronas2 + 1; j++)
                {
                    if (j != N_neuronas2)
                        genotipo_3[i, j] = neuronas[2].N[i].getAW(j);

                    else
                        genotipo_3[i, N_neuronas2] = neuronas[2].N[i].getB();

                }
            }
        }

        //  Método de actualización de neuronas respecto a la información genética
        public void actualizarCromosoma()
        {
            for (int i = 0; i < genotipo_1.GetLength(0); i++)
            {
                for (int j = 0; j < genotipo_1.GetLength(1); j++)
                {

                    if (j + 1 == genotipo_1.GetLength(1))
                    {
                        neuronas[0].N[i].setB(genotipo_1[i, j]);
                    }
                    else if (j < genotipo_1.GetLength(1) - 1)
                        neuronas[0].N[i].setAW(genotipo_1[i, j], j);
                }
            }

            for (int i = 0; i < genotipo_2.GetLength(0); i++)
            {
                for (int j = 0; j < genotipo_2.GetLength(1); j++)
                {

                    if (j + 1 == genotipo_2.GetLength(1))
                    {
                        neuronas[1].N[i].setB(genotipo_2[i, j]);
                    }
                    else if (j < genotipo_2.GetLength(1) - 1)
                        neuronas[1].N[i].setAW(genotipo_2[i, j], j);
                }
            }

            for (int i = 0; i < genotipo_3.GetLength(0); i++)
            {
                for (int j = 0; j < genotipo_3.GetLength(1); j++)
                {

                    if (j + 1 == genotipo_3.GetLength(1))
                    {
                        neuronas[2].N[i].setB(genotipo_3[i, j]);
                    }
                    else if (j < genotipo_3.GetLength(1) - 1)
                        neuronas[2].N[i].setAW(genotipo_3[i, j], j);
                }
            }
        }
    }
}
