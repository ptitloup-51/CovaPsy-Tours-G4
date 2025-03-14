
#include "main.h"
#include "adc.h"
#include "i2c.h"
#include "spi.h"
#include "tim.h"
#include "usart.h"
#include "gpio.h"

#include "ssd1306.h"
#include "fonts.h"
#include "stm32g4xx.h"
#include <stdio.h>
#include <string.h>


#define DISTANCE_1_TOUR_AXE_TRANSMISSION_MM 79

#define R1 1000.0f
#define R2 560.0f


char buffer1[20];
char buffer2[20];

float vitesse_mesuree_m_s = 0;
uint32_t mesure_us = 0;
uint32_t mesure_precedente_us = 0;
volatile uint8_t vitesse_a_mettre_a_jour = 0;

uint8_t rxBuffer[4]; // Buffer de réception du maître (4 octets)
uint8_t txBuffer[4]; // Buffer de transmission (4 octets)


void SystemClock_Config(void);

void HAL_TIM_IC_CaptureCallback(TIM_HandleTypeDef* htim)
{
	// Récupération de la vitesse à chaque interruption (1/16e de tour de roue)
	mesure_us = __HAL_TIM_GET_COMPARE(&htim2, TIM_CHANNEL_1); // ou TIM2->CCR1

	vitesse_mesuree_m_s = DISTANCE_1_TOUR_AXE_TRANSMISSION_MM *1000 / 16.0 / (mesure_us - mesure_precedente_us);

	mesure_precedente_us = mesure_us;

    // On indique qu'une nouvelle vitesse est disponible
    vitesse_a_mettre_a_jour = 1;
}
void MettreAJourEcran(void);
void envoyerMessage();

int main(void)
{
  HAL_Init();

  SystemClock_Config();

  MX_GPIO_Init();
  MX_TIM1_Init();
  MX_TIM2_Init();
  MX_USART1_UART_Init();
  MX_TIM6_Init();
  MX_TIM3_Init();
  MX_I2C1_Init();
  MX_SPI3_Init();
  MX_ADC1_Init();
  /* USER CODE BEGIN 2 */

  HAL_TIM_IC_Start_IT(&htim2, TIM_CHANNEL_1);
  HAL_NVIC_SetPriority(TIM2_IRQn, 1, 0);
  HAL_NVIC_EnableIRQ(TIM2_IRQn);

  HAL_ADC_Start(&hadc1);

  SSD1306_Init();
  SSD1306_Clear();
  SSD1306_GotoXY(10, 5);
  SSD1306_Puts("Init OK", &Font_11x18, SSD1306_COLOR_WHITE);
  SSD1306_UpdateScreen();
  HAL_Delay(1000);
  SSD1306_Clear();
  SSD1306_GotoXY(5, 5);
  sprintf(buffer1, "Vit : %.2f m/s", (double)vitesse_mesuree_m_s);
  SSD1306_Puts(buffer1, &Font_7x10, SSD1306_COLOR_WHITE);
  SSD1306_UpdateScreen();

  while (1)
  {
      MettreAJourEcran(); // Mise à jour de l'affichage (batterie + vitesse)

      envoyerMessage();
  }

}

void SystemClock_Config(void)
{
  RCC_OscInitTypeDef RCC_OscInitStruct = {0};
  RCC_ClkInitTypeDef RCC_ClkInitStruct = {0};

  HAL_PWREx_ControlVoltageScaling(PWR_REGULATOR_VOLTAGE_SCALE1_BOOST);

  RCC_OscInitStruct.OscillatorType = RCC_OSCILLATORTYPE_HSI;
  RCC_OscInitStruct.HSIState = RCC_HSI_ON;
  RCC_OscInitStruct.HSICalibrationValue = RCC_HSICALIBRATION_DEFAULT;
  RCC_OscInitStruct.PLL.PLLState = RCC_PLL_ON;
  RCC_OscInitStruct.PLL.PLLSource = RCC_PLLSOURCE_HSI;
  RCC_OscInitStruct.PLL.PLLM = RCC_PLLM_DIV4;
  RCC_OscInitStruct.PLL.PLLN = 85;
  RCC_OscInitStruct.PLL.PLLP = RCC_PLLP_DIV2;
  RCC_OscInitStruct.PLL.PLLQ = RCC_PLLQ_DIV2;
  RCC_OscInitStruct.PLL.PLLR = RCC_PLLR_DIV2;
  if (HAL_RCC_OscConfig(&RCC_OscInitStruct) != HAL_OK)
  {
    Error_Handler();
  }

  RCC_ClkInitStruct.ClockType = RCC_CLOCKTYPE_HCLK|RCC_CLOCKTYPE_SYSCLK
                              |RCC_CLOCKTYPE_PCLK1|RCC_CLOCKTYPE_PCLK2;
  RCC_ClkInitStruct.SYSCLKSource = RCC_SYSCLKSOURCE_PLLCLK;
  RCC_ClkInitStruct.AHBCLKDivider = RCC_SYSCLK_DIV1;
  RCC_ClkInitStruct.APB1CLKDivider = RCC_HCLK_DIV1;
  RCC_ClkInitStruct.APB2CLKDivider = RCC_HCLK_DIV1;

  if (HAL_RCC_ClockConfig(&RCC_ClkInitStruct, FLASH_LATENCY_4) != HAL_OK)
  {
    Error_Handler();
  }
}

void MettreAJourEcran(void)
{
    /* Affichage de la vitesse */
    if (vitesse_a_mettre_a_jour)
    {
        SSD1306_GotoXY(5, 5);
        sprintf(buffer1, "Vit : %.2f m/s", (double)vitesse_mesuree_m_s);
        SSD1306_Puts(buffer1, &Font_7x10, SSD1306_COLOR_WHITE);
        vitesse_a_mettre_a_jour = 0;
    }

    /* Lecture et affichage de la batterie */
    if (HAL_ADC_PollForConversion(&hadc1, HAL_MAX_DELAY) == HAL_OK)
    {
    	uint32_t adcValue = HAL_ADC_GetValue(&hadc1);
    	float adcVoltage = adcValue * 3.3f / 4095.0f;
    	float batteryVoltage = adcVoltage * 10;

        SSD1306_GotoXY(10, 40);
        sprintf(buffer2, "Battery: %.2fV", (double)batteryVoltage);
        SSD1306_Puts(buffer2, &Font_7x10, 1);
    }

    /* Mise à jour de l'affichage */
    SSD1306_UpdateScreen();
    HAL_Delay(50);
}
void envoyerMessage()
{
    memset(txBuffer, 0, sizeof(txBuffer)); // Nettoyage du buffer avant l'envoi
    memset(rxBuffer, 0, sizeof(rxBuffer)); // Nettoyage du buffer de réception

    // Convertir la vitesse en 4 octets (float -> bytes)
    memcpy(txBuffer, &vitesse_mesuree_m_s, sizeof(float));

    // Envoyer la vitesse au maître
    HAL_SPI_TransmitReceive(&hspi3, txBuffer, rxBuffer, sizeof(txBuffer), 100);
}

