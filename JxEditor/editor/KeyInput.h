#pragma once

#include "../pico/pico.types.h"

extern const struct KeyInput {
	/**
	* @brief �L�[���͂𕶎���z��s�Ɋi�[
	* @param str �������������̓��͌��ʎ擾�z��(������������g�p���Ȃ��ꍇ��*str=0)
	* @param sz �o�b�t�@�T�C�Y
	* @retval true: Enter�ŏI��(OK)�Afalse:ESC�ŏI��(�L�����Z��)
	* �J�[�\���ʒu��setcursor�֐��Ŏw�肵�Ă���
	*/
	bool_t (*gets)(char16_t *s, size32_t sz);

	// �L�[�{�[�h����1�L�[���͑҂�(ASCII�R�[�h��������L�[�Ɍ���)
	// �߂�l �ʏ핶���̏ꍇASCII�R�[�h
	uint8_t(*getch)(void);

	// �L�[�{�[�h����ʏ핶���L�[���͑҂����A���͂��ꂽ������\��
	// �߂�l ���͂��ꂽ������ASCII�R�[�h�A�O���[�o���ϐ�vkey�ɍŌ�ɉ����ꂽ�L�[�̉��z�L�[�R�[�h
	uint8_t (*getchar)(void);

	// �}�����[�h�Ftrue�A�㏑�����[�h�Ffalse
	bool_t (*GetInsertMode)(void);
	void (*SetInsertMode)(bool_t mode);

} KeyInput;
