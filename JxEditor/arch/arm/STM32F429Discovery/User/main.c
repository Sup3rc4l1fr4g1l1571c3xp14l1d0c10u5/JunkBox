/**
 * Keil project template
 *
 * Before you start, select your target, on the right of the "Load" button
 *
 * @author    Tilen Majerle
 * @email     tilen@majerle.eu
 * @website   http://stm32f4-discovery.com
 * @ide		  Keil uVision 5
 * @conf      System clock configured for USB if needed
 * @conf      STM32F429 and STM32F446 are set to 168MHz and STM32F411xx to 96MHz for USB 48MHz clock
 * @conf      PLL parameters are set in "Options for Target" -> "C/C++" -> "Defines"
 * @packs     STM32F4xx Keil packs version 2.4.0 or greater required
 * @stdperiph STM32F4xx Standard peripheral drivers version 1.5.0 or greater required
 */
/* Include core modules */
#include "stm32f4xx.h"
/* Include my libraries here */
#include "defines.h"
#include "tm_stm32f4_delay.h"
#include "tm_stm32f4_disco.h"
#include "tm_stm32f4_sdram.h"
#include "tm_stm32f4_ili9341_ltdc.h"
#include "tm_stm32f4_fonts.h"
#include "tm_stm32f4_usb_hid_host.h"

#include "../te/editor/TextEditor.h"
#include "../te/editor/TextBuffer.h"
#include "../te/editor/Keyboard.h"

#include "usbhid_scancode.h"

uint8_t * const __heap_start = (uint8_t *)(SDRAM_START_ADR + 320U*240U*sizeof(uint16_t)*2U);
uint8_t * const __heap_end   = (uint8_t *)(SDRAM_START_ADR + SDRAM_MEMORY_SIZE);

static TM_USB_HIDHOST_Result_t USB_HID_Status;		/* USB HID Host status */
static TM_USB_HIDHOST_Keyboard_t Keyboard_Data;	/* Keyboard handler */

int main(void) {
	__disable_irq();

	SystemInit();

	/* Initialize delay */
	TM_DELAY_Init();

	/* Initialize leds */
	TM_DISCO_LedInit();	

	/* Initialize USB HID HOST */    
	TM_USB_HIDHOST_Init();

	// SDRAMÇ‡èâä˙âª
	TM_ILI9341_Init();
	//Rotate LCD for 90 degrees
	TM_ILI9341_Rotate(TM_ILI9341_Orientation_Landscape_2);

	TM_ILI9341_SetLayer1();
	/* Fill data on layer 1 */
	TM_ILI9341_Fill(ILI9341_COLOR_WHITE);

	__enable_irq();

	texteditor();

	for (;;) { }
}

#define SCALE 1
#define VIDEO_WIDTH_PIXEL 320
#define VIDEO_HEIGHT_PIXEL 240

/* ÉsÉNÉZÉãëÄçÏ */
static void set_pixel(uint16_t x, uint16_t y, uint32_t c) {
	int yy = (int)y * SCALE;
	for (int i = 0; i < SCALE; i++) {
		if (yy >= VIDEO_HEIGHT_PIXEL*SCALE) { break; }
		int xx = (int)x * SCALE;
		for (int j = 0; j < SCALE; j++) {
			if (xx >= VIDEO_WIDTH_PIXEL*SCALE) { break; }
			TM_ILI9341_DrawPixel(xx, yy, c);
			xx++;
		}
		yy++;
	}
}

void draw_video() {
	static uint32_t cnt = 0U;
	if (cnt++ != 0) {
		if (cnt == 20) {
			cnt = 0;
		}
		return;
	}
	
	text_vram_pixel_t* cursor = TextVRAM.GetCursorPtr();
	text_vram_pixel_t* tvram  = TextVRAM.GetVramPtr();
	const color_t *ClTable = TextVRAM.GetPalettePtr();

	for (int y = 0; y < WIDTH_Y; y++) {
		text_vram_pixel_t *line = &tvram[y*VWIDTH_X];

		for (int x = 0; x < VWIDTH_X;) {
			uint16_t id = Uni2Id(line[x].ch).id;
			int sz = 1;
			color_t c = ClTable[line[x].color];
			color_t b = ClTable[line[x].bgcolor];
			unsigned int inv = (&line[x] == cursor) ? 0x00FFFFFF : 0x00000000;
			if (id == 0xFFFFU) {
				for (int j = 0; j < FONT_HEIGHT; j++) {
					for (int i = 0; i < FONT_WIDTH/2; i++) {
					set_pixel(x*FONT_WIDTH / 2 + i, y*FONT_HEIGHT + j, b.bgra ^ inv);
					}
				}
			}else {
					for (int j = 0; j < FONT_HEIGHT; j++) {
					const uint8_t *p = FontData[id];
					sz = p[0];
					for (int i = 0; i < FONT_WIDTH/2* sz; i++) {
						set_pixel(x*FONT_WIDTH / 2 + i, y*FONT_HEIGHT + j, ((p[i / 8 + j * 2 + 1] >> (i % 8)) & 0x01 ? c.bgra : b.bgra) ^ inv);
					}
				}
			}
			x += sz;
		}
	}
}

static void USB_HIDHOST_process(void) {
//	static uint32_t cnt = 0U;
//	if (cnt++ != 0) {
//		if (cnt == 10) {
//			cnt = 0;
//		}
//		return;
//	}

		/* Process USB HID */
	/* This must be called periodically */
	TM_USB_HIDHOST_Process();

	/* Get connected device */
	USB_HID_Status = TM_USB_HIDHOST_Device();

	/* Switch status */
	switch (USB_HID_Status) {
		/* Keyboard connected */
		case TM_USB_HIDHOST_Result_KeyboardConnected:
			
			/* GREEN led ON */
			TM_DISCO_LedOn(LED_GREEN);
		
			/* Get keyboard data */
			TM_USB_HIDHOST_ReadKeyboard(&Keyboard_Data);
			/* Check if keyboard button is pressed */
		
		for (int i=0; i<14*2; i++) {
			if (Keyboard_Data.state[i].ButtonStatus == TM_USB_HIDHOST_Button_Pressed) {
				Keyboard_PushKeyStatus((VIRTUAL_KEY)usbhidscancode2virtualkey[Keyboard_Data.state[i].Button], false);
			} else if (Keyboard_Data.state[i].ButtonStatus == TM_USB_HIDHOST_Button_Released) {
				Keyboard_PushKeyStatus((VIRTUAL_KEY)usbhidscancode2virtualkey[Keyboard_Data.state[i].Button], true);
			}
		}
			break;
			
		/* Mouse connected */
		case TM_USB_HIDHOST_Result_MouseConnected:
			break;
		
		/* No device connected */
		case TM_USB_HIDHOST_Result_Disconnected:
			break;
		
		/* Device is not supported */
		case TM_USB_HIDHOST_Result_DeviceNotSupported:
			break;
		
		/* Error occurred somewhere */
		case TM_USB_HIDHOST_Result_Error:
			break;
		
		/* Library is not initialized */
		case TM_USB_HIDHOST_Result_LibraryNotInitialized:
			break;
	}
}

/* User handler for 1ms interrupts */
void TM_DELAY_1msHandler(void) {
	USB_HIDHOST_process();
	draw_video();
}
