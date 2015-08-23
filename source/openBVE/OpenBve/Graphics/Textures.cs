﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

namespace OpenBve {
	/// <summary>Provides functions for dealing with textures.</summary>
	internal static partial class Textures {
		
		// --- members ---
		
		/// <summary>Holds all currently registered textures.</summary>
		private static Texture[] RegisteredTextures = new Texture[16];
		
		/// <summary>The number of currently registered textures.</summary>
		private static int RegisteredTexturesCount = 0;
		
		
		// --- initialize / deinitialize ---
		
		/// <summary>Initializes the texture component. A call to Deinitialize must be made when terminating the program.</summary>
		internal static void Initialize() {
		}
		
		/// <summary>Deinitializes the texture component.</summary>
		internal static void Deinitialize() {
			UnloadAllTextures();
		}
		
		
		// --- register texture ---

		/// <summary>Registeres a texture and returns a handle to the texture.</summary>
		/// <param name="path">The path to the file or directory that contains the texture.</param>
		/// <param name="handle">Receives a handle to the texture.</param>
		/// <returns>Whether registering the texture was successful.</returns>
		internal static bool RegisterTexture(string path, out Texture handle) {
			return RegisterTexture(path, null, out handle);
		}
		
		/// <summary>Registeres a texture and returns a handle to the texture.</summary>
		/// <param name="path">The path to the texture.</param>
		/// <param name="parameters">The parameters that specify how to process the texture.</param>
		/// <param name="handle">Receives a handle to the texture.</param>
		/// <returns>Whether registering the texture was successful.</returns>
		internal static bool RegisterTexture(string path, OpenBveApi.Textures.TextureParameters parameters, out Texture handle) {
			/*
			 * Check if the texture is already registered.
			 * If so, return the existing handle.
			 * */
			for (int i = 0; i < RegisteredTexturesCount; i++) {
				PathOrigin source = RegisteredTextures[i].Origin as PathOrigin;
				if (source != null && source.Path == path && source.Parameters == parameters) {
					handle = RegisteredTextures[i];
					return true;
				}
			}
			/*
			 * Register the texture and return the newly created handle.
			 * */
			if (RegisteredTextures.Length == RegisteredTexturesCount) {
				Array.Resize<Texture>(ref RegisteredTextures, RegisteredTextures.Length << 1);
			}
			RegisteredTextures[RegisteredTexturesCount] = new Texture(path, parameters);
			RegisteredTexturesCount++;
			handle = RegisteredTextures[RegisteredTexturesCount - 1];
			return true;
		}
		
		/// <summary>Registeres a texture and returns a handle to the texture.</summary>
		/// <param name="texture">The texture data.</param>
		/// <returns>The handle to the texture.</returns>
		internal static Texture RegisterTexture(OpenBveApi.Textures.Texture texture) {
			/*
			 * Register the texture and return the newly created handle.
			 * */
			if (RegisteredTextures.Length == RegisteredTexturesCount) {
				Array.Resize<Texture>(ref RegisteredTextures, RegisteredTextures.Length << 1);
			}
			RegisteredTextures[RegisteredTexturesCount] = new Texture(texture);
			RegisteredTexturesCount++;
			return RegisteredTextures[RegisteredTexturesCount - 1];
		}
		
		/// <summary>Registeres a texture and returns a handle to the texture.</summary>
		/// <param name="bitmap">The bitmap that contains the texture.</param>
		/// <returns>The handle to the texture.</returns>
		/// <remarks>Be sure not to dispose of the bitmap after calling this function.</remarks>
		internal static Texture RegisterTexture(Bitmap bitmap) {
			/*
			 * Register the texture and return the newly created handle.
			 * */
			if (RegisteredTextures.Length == RegisteredTexturesCount) {
				Array.Resize<Texture>(ref RegisteredTextures, RegisteredTextures.Length << 1);
			}
			RegisteredTextures[RegisteredTexturesCount] = new Texture(bitmap);
			RegisteredTexturesCount++;
			return RegisteredTextures[RegisteredTexturesCount - 1];
		}

		
		// --- load texture ---
		
		/// <summary>Loads the specified texture into OpenGL if not already loaded.</summary>
		/// <param name="handle">The handle to the registered texture.</param>
		/// <param name="wrap">The texture type indicating the clamp mode.</param>
		/// <returns>Whether loading the texture was successful.</returns>
		internal static bool LoadTexture(Texture handle, OpenGlTextureWrapMode wrap){
			if (handle.OpenGlTextures[(int)wrap].Valid)
				return true;
			if (handle.Ignore)
				return false;
			else {
				OpenBveApi.Textures.Texture texture;
				if (handle.Origin.GetTexture(out texture)) {
					if (texture.BitsPerPixel == 32) {
						int[] names = new int[1];
						GL.GenTextures(1, names);
						ErrorCode error = GL.GetError();
						GL.BindTexture(TextureTarget.Texture2D, names[0]);
						error = GL.GetError();
						handle.OpenGlTextures[(int)wrap].Name = names[0];
						handle.Width = texture.Width;
						handle.Height = texture.Height;
						handle.Transparency = texture.GetTransparencyType();
						texture = UpsizeToPowerOfTwo(texture);
						
//						int newWidth = Math.Min(texture.Width, 256);
//						int newHeight = Math.Min(texture.Height, 256);
//						texture = Resize(texture, newWidth, newHeight);
						
						switch (Options.Current.Interpolation) {
							case Options.InterpolationMode.NearestNeighbor:
								GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Nearest);
								GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Nearest);
								break;
							case Options.InterpolationMode.Bilinear:
								GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.Linear);
								GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);
								break;
							case Options.InterpolationMode.NearestNeighborMipmapped:
								GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.NearestMipmapNearest);
								GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Nearest);
								break;
							case Options.InterpolationMode.BilinearMipmapped:
								GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.NearestMipmapLinear);
								GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);
								break;
							case Options.InterpolationMode.TrilinearMipmapped:
								GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.LinearMipmapLinear);
								GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);
								break;
							default:
								GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (float)TextureMinFilter.LinearMipmapLinear);
								GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (float)TextureMagFilter.Linear);
								break;
						}
						if ((wrap & OpenGlTextureWrapMode.RepeatClamp) != 0) {
							GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.Repeat);
						} else {
							GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (float)TextureWrapMode.ClampToEdge);
						}
						if ((wrap & OpenGlTextureWrapMode.ClampRepeat) != 0) {
							GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureWrapMode.Repeat);
						} else {
							GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (float)TextureWrapMode.ClampToEdge);
						}
						if (Options.Current.Interpolation == Options.InterpolationMode.NearestNeighbor && Options.Current.Interpolation == Options.InterpolationMode.Bilinear) {
							GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, 0);
						} else {
							GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, 1);
						}
						if (Options.Current.Interpolation == Options.InterpolationMode.AnisotropicFiltering) {
							GL.TexParameter(TextureTarget.Texture2D, (TextureParameterName)ExtTextureFilterAnisotropic.TextureMaxAnisotropyExt, Options.Current.AnisotropicFilteringLevel);
						}
						if (handle.Transparency == OpenBveApi.Textures.TextureTransparencyType.Opaque) {
							/*
							 * If the texture is fully opaque, the alpha channel is not used.
							 * If the graphics driver and card support 24-bits per channel,
							 * it is best to convert the bitmap data to that format in order
							 * to save memory on the card. If the card does not support the
							 * format, it will likely be upconverted to 32-bits per channel
							 * again, and this is wasted effort.
							 * */
							int width = texture.Width;
							int height = texture.Height;
							int stride = (3 * (width + 1) >> 2) << 2;
							byte[] oldBytes = texture.Bytes;
							byte[] newBytes = new byte[stride * texture.Height];
							int i = 0, j = 0;
							for (int y = 0; y < height; y++) {
								for (int x = 0; x < width; x++) {
									newBytes[j + 0] = oldBytes[i + 0];
									newBytes[j + 1] = oldBytes[i + 1];
									newBytes[j + 2] = oldBytes[i + 2];
									i += 4;
									j += 3;
								}
								j += stride - 3 * width;
							}
							GL.TexImage2D(TextureTarget.Texture2D, 0, 
								PixelInternalFormat.Rgb8, 
								texture.Width, texture.Height, 0,
								OpenTK.Graphics.OpenGL.PixelFormat.Rgb, 
								PixelType.UnsignedByte, newBytes);
						} else {
							/*
							 * The texture uses its alpha channel, so send the bitmap data
							 * in 32-bits per channel as-is.
							 * */
							GL.TexImage2D(TextureTarget.Texture2D, 0, 
								PixelInternalFormat.Rgba8, 
								texture.Width, texture.Height, 0, 
								OpenTK.Graphics.OpenGL.PixelFormat.Rgba, 
								PixelType.UnsignedByte, texture.Bytes);
						}
						handle.OpenGlTextures[(int)wrap].Valid = true;
						return true;
					}
				}
			}
			handle.Ignore = true;
			return false;
		}
		
		/// <summary>Loads all registered textures.</summary>
		internal static void LoadAllTextures() {
//			for (int i = 0; i < RegisteredTexturesCount; i++) {
//				LoadTexture(RegisteredTextures[i]);
//			}
		}
		
		
		// --- save texture ---
		
		/// <summary>Saves a texture to a file.</summary>
		/// <param name="file">The file.</param>
		/// <param name="texture">The texture.</param>
		/// <remarks>The texture is always saved in PNG format.</remarks>
		internal static void SaveTexture(string file, OpenBveApi.Textures.Texture texture) {
			Bitmap bitmap = new Bitmap(texture.Width, texture.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			BitmapData data = bitmap.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
			byte[] bytes = new byte[texture.Bytes.Length];
			for (int i = 0; i < bytes.Length; i += 4) {
				bytes[i] = texture.Bytes[i + 2];
				bytes[i + 1] = texture.Bytes[i + 1];
				bytes[i + 2] = texture.Bytes[i];
				bytes[i + 3] = texture.Bytes[i + 3];
			}
			Marshal.Copy(bytes, 0, data.Scan0, texture.Bytes.Length);
			bitmap.UnlockBits(data);
			bitmap.Save(file, ImageFormat.Png);
			bitmap.Dispose();
		}

		
		// --- upsize texture ---
		
		/// <summary>Upsizes the specified texture to a power of two size and returns the result.</summary>
		/// <param name="texture">The texture.</param>
		/// <returns>The upsized texture, or the original if already a power of two size.</returns>
		/// <exception cref="System.NotSupportedException">The bits per pixel in the texture is not supported.</exception>
		internal static OpenBveApi.Textures.Texture UpsizeToPowerOfTwo(OpenBveApi.Textures.Texture texture) {
			int width = RoundUpToPowerOfTwo(texture.Width);
			int height = RoundUpToPowerOfTwo(texture.Height);
			return Resize(texture, width, height);
		}
		
		/// <summary>Resizes the specified texture to the specified width and height and returns the result.</summary>
		/// <param name="texture">The texture.</param>
		/// <param name="width">The new width.</param>
		/// <param name="height">The new height.</param>
		/// <returns>The resize texture, or the original if already of the specified size.</returns>
		/// <exception cref="System.NotSupportedException">The bits per pixel in the texture is not supported.</exception>
		internal static OpenBveApi.Textures.Texture Resize(OpenBveApi.Textures.Texture texture, int width, int height){
			if (width == texture.Width && height == texture.Height)
				return texture;
			if (texture.BitsPerPixel != 32)
				throw new NotSupportedException("The number of bits per pixel is not supported.");
			else {
				OpenBveApi.Textures.TextureTransparencyType type = texture.GetTransparencyType();
				/*
				 * Convert the texture into a bitmap.
				 * */
				Bitmap bitmap = new Bitmap(texture.Width, texture.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				BitmapData data = bitmap.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.WriteOnly, bitmap.PixelFormat);
				Marshal.Copy(texture.Bytes, 0, data.Scan0, texture.Bytes.Length);
				bitmap.UnlockBits(data);
				/*
				 * Scale the bitmap.
				 * */
				Bitmap scaledBitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				Graphics graphics = Graphics.FromImage(scaledBitmap);
				graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
				graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				graphics.DrawImage(bitmap, new Rectangle(0, 0, width, height), new Rectangle(0, 0, texture.Width, texture.Height), GraphicsUnit.Pixel);
				graphics.Dispose();
				bitmap.Dispose();
				/*
				 * Convert the bitmap into a texture.
				 * */
				data = scaledBitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, scaledBitmap.PixelFormat);
				byte[] bytes = new byte[4 * width * height];
				Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
				scaledBitmap.UnlockBits(data);
				scaledBitmap.Dispose();
				/*
				 * Ensure opaque and partially transparent
				 * textures have valid alpha components.
				 * */
				if (type == OpenBveApi.Textures.TextureTransparencyType.Opaque) {
					for (int i = 3; i < bytes.Length; i += 4) {
						bytes[i] = 255;
					}
				} else if (type == OpenBveApi.Textures.TextureTransparencyType.Partial) {
					for (int i = 3; i < bytes.Length; i += 4) {
						bytes[i] = bytes[i] < 128 ? (byte)0 : (byte)255;
					}
				}
				OpenBveApi.Textures.Texture result = new OpenBveApi.Textures.Texture(width, height, 32, bytes);
				return result;
			}
		}
		
		
		// --- unload texture ---
		
		/// <summary>Unloads the specified texture from OpenGL if loaded.</summary>
		/// <param name="handle">The handle to the registered texture.</param>
		internal static void UnloadTexture(Texture handle) {
			for (int i = 0; i < handle.OpenGlTextures.Length; i++) {
				if (handle.OpenGlTextures[i].Valid) {
					GL.DeleteTextures(1, new int[] { handle.OpenGlTextures[i].Name });
					handle.OpenGlTextures[i].Valid = false;
				}
			}
			handle.Ignore = false;
		}

		/// <summary>Unloads all registered textures.</summary>
		internal static void UnloadAllTextures() {
			for (int i = 0; i < RegisteredTexturesCount; i++) {
				UnloadTexture(RegisteredTextures[i]);
			}
		}
		
		
		// --- statistics ---
		
		/// <summary>Gets the number of registered textures.</summary>
		/// <returns>The number of registered textures.</returns>
		internal static int GetNumberOfRegisteredTextures() {
			return RegisteredTexturesCount;
		}

		/// <summary>Gets the number of loaded textures.</summary>
		/// <returns>The number of loaded textures.</returns>
		internal static int GetNumberOfLoadedTextures() {
			int count = 0;
			for (int i = 0; i < RegisteredTexturesCount; i++) {
				for (int j = 0; j < RegisteredTextures[i].OpenGlTextures.Length; j++) {
					if (RegisteredTextures[i].OpenGlTextures[j].Valid) {
						count++;
						break;
					}
				}
			}
			return count;
		}
		
		
		// --- functions ---
		
		/// <summary>Takes a positive value and rounds it up to the next highest power of two.</summary>
		/// <param name="value">The value.</param>
		/// <returns>The next highest power of two, or the original value if already a power of two.</returns>
		internal static int RoundUpToPowerOfTwo(int value) {
			if (value <= 0)
				throw new ArgumentException("The specified value is not positive.");
			value -= 1;
			for (int i = 1; i < sizeof(int) * 8; i <<= 1) {
				value = value | value >> i;
			}
			return value + 1;

		}
	}
}